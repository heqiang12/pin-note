using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using PinNote.Models;
using PinNote.Services;

namespace PinNote;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "PinNote";

    private readonly AppData _data;
    private readonly ObservableCollection<NoteItem> _notes;
    private readonly ListCollectionView _view;
    private readonly List<ThemePreset> _themes;
    private readonly DispatcherTimer _saveTimer;

    private bool _loading = true;
    private bool _collapsed;
    private double _expandWidth;
    private double _expandHeight;

    public MainWindow()
    {
        InitializeComponent();

        _data = Store.Load();
        _expandWidth = Math.Max(260, _data.Settings.Width);
        _expandHeight = Math.Max(200, _data.Settings.Height);

        _notes = new ObservableCollection<NoteItem>(_data.Notes);
        foreach (var n in _notes) n.PropertyChanged += Note_PropertyChanged;

        _view = new ListCollectionView(_notes);
        _view.SortDescriptions.Add(new SortDescription(nameof(NoteItem.Status), ListSortDirection.Ascending));
        _view.SortDescriptions.Add(new SortDescription(nameof(NoteItem.CreatedAt), ListSortDirection.Descending));
        _view.IsLiveSorting = true;
        _view.LiveSortingProperties.Add(nameof(NoteItem.Status));
        _view.LiveSortingProperties.Add(nameof(NoteItem.CreatedAt));

        _themes = ThemePreset.All.ToList();
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); Flush(); };

        NoteList.ItemsSource = _view;
        ThemeList.ItemsSource = _themes;
        DataContext = this;

        ApplySettings(save: false);
        _loading = false;

        Loaded += (_, _) => InputBox.Focus();
        LocationChanged += (_, _) => ScheduleSave();
        SizeChanged += (_, _) => ScheduleSave();
    }

    // ---------- 绑定属性 ----------

    public List<ThemePreset> Themes => _themes;

    public int PendingCount => _notes.Count(n => n.Status != NoteStatus.Done);

    public string PendingText => PendingCount == 0
        ? (_notes.Count == 0 ? "还没有记录" : "都做完了")
        : $"{PendingCount} 条待做";

    public string OpacityLabel => $"{(int)Math.Round(_data.Settings.OpacityPercent)}%";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void UpdateCounters()
    {
        Raise(nameof(PendingCount));
        Raise(nameof(PendingText));
        EmptyHint.Visibility = _notes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // ---------- 设置与外观 ----------

    private void ApplySettings(bool save)
    {
        var s = _data.Settings;

        Width = _expandWidth;
        Height = _expandHeight;
        Topmost = s.Topmost;

        // 位置：第一次启动贴右上角，之后记住上次位置
        if (s.Left < 0 || s.Top < 0)
        {
            PlaceAtTopRight();
        }
        else
        {
            Left = ClampToScreen(s.Left, Width, SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenWidth);
            Top = ClampToScreen(s.Top, Height, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenHeight);
        }

        var theme = ThemePreset.Get(s.Theme);
        foreach (var t in _themes) t.IsSelected = t.Id == theme.Id;

        ApplyTheme(theme, s.OpacityPercent);
        OpacitySlider.Value = Math.Clamp(s.OpacityPercent, 20, 100);
        TopmostCheck.IsChecked = s.Topmost;
        AutoStartCheck.IsChecked = IsAutoStartEnabled();
        s.AutoStart = AutoStartCheck.IsChecked == true;

        if (s.Collapsed) CollapseSilently();

        UpdateCounters();
        Raise(nameof(OpacityLabel));

        if (save) ScheduleSave();
    }

    private void PlaceAtTopRight()
    {
        var wa = SystemParameters.WorkArea;
        var w = Math.Min(Width, Math.Max(260, wa.Width - 40));
        var h = Math.Min(Height, Math.Max(200, wa.Height - 40));

        Width = w;
        Height = h;
        _expandWidth = w;
        _expandHeight = h;

        Left = wa.Right - w - 18;
        Top = wa.Top + 18;
    }

    private static double ClampToScreen(double value, double size, double screenStart, double screenSize)
    {
        var max = screenStart + screenSize - size - 8;
        return Math.Clamp(value, screenStart + 8, Math.Max(screenStart + 8, max));
    }

    private void ApplyTheme(ThemePreset theme, double opacityPercent)
    {
        var alpha = (byte)Math.Clamp(opacityPercent / 100.0 * 255.0, 25, 255);

        SetBrush("ThemeBgBrush", Color.FromArgb(alpha, theme.Background.R, theme.Background.G, theme.Background.B));
        SetBrush("ThemeTextBrush", theme.Text);
        SetBrush("ThemeSubBrush", theme.Sub);
        SetBrush("ThemeBorderBrush", theme.Border);
        SetBrush("ThemeHoverBrush", theme.Hover);
        SetBrush("ThemeFieldBrush", theme.Field);
        SetBrush("ThemeSolidBgBrush", theme.Background);
        SetBrush("ThemePanelBrush", Shift(theme.Background, theme.IsDark ? 13 : -9));
        SetBrush("ThemeScrollBrush", theme.IsDark
            ? Color.FromArgb(0x38, 255, 255, 255)
            : Color.FromArgb(0x30, 0, 0, 0));
    }

    /// <summary>把颜色整体提亮或压暗一点，用来做面板分层。</summary>
    private static Color Shift(Color c, int delta) => Color.FromArgb(
        252,
        (byte)Math.Clamp(c.R + delta, 0, 255),
        (byte)Math.Clamp(c.G + delta, 0, 255),
        (byte)Math.Clamp(c.B + delta, 0, 255));

    private void SetBrush(string key, Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        Resources[key] = brush;
    }

    // ---------- 输入 ----------

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        => Placeholder.Visibility = InputBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

    private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            CommitInput();
        }
        else if (e.Key == Key.Escape)
        {
            SettingsOverlay.Visibility = Visibility.Collapsed;
            InputBox.Clear();
        }
    }

    private void CommitInput()
    {
        var text = InputBox.Text.Trim();
        if (text.Length == 0) return;

        var note = new NoteItem
        {
            Text = text,
            CreatedAt = DateTime.Now,
            Status = NoteStatus.Todo
        };

        note.PropertyChanged += Note_PropertyChanged;
        _notes.Add(note);
        InputBox.Clear();

        UpdateCounters();
        ScheduleSave();
        ListScroll.ScrollToTop();
    }

    // ---------- 列表操作 ----------

    private void Note_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(NoteItem.Status) or nameof(NoteItem.TimeText))
        {
            UpdateCounters();
            ScheduleSave();
        }
    }

    private void Status_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: NoteItem note }) return;

        note.Status = note.Status switch
        {
            NoteStatus.Todo => NoteStatus.Doing,
            NoteStatus.Doing => NoteStatus.Done,
            _ => NoteStatus.Todo
        };
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: NoteItem note }) return;

        note.PropertyChanged -= Note_PropertyChanged;
        _notes.Remove(note);
        UpdateCounters();
        ScheduleSave();
    }

    private void ClearDone_Click(object sender, RoutedEventArgs e)
    {
        var done = _notes.Where(n => n.Status == NoteStatus.Done).ToList();
        if (done.Count == 0) return;

        var answer = MessageBox.Show($"删掉 {done.Count} 条已完成的记录？", "PinNote",
            MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
        if (answer != MessageBoxResult.OK) return;

        foreach (var note in done)
        {
            note.PropertyChanged -= Note_PropertyChanged;
            _notes.Remove(note);
        }

        UpdateCounters();
        ScheduleSave();
    }

    // ---------- 设置面板 ----------

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var show = SettingsOverlay.Visibility != Visibility.Visible;
        SettingsOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (show)
        {
            OpacitySlider.Value = Math.Clamp(_data.Settings.OpacityPercent, 20, 100);
            TopmostCheck.IsChecked = Topmost;
            AutoStartCheck.IsChecked = IsAutoStartEnabled();
        }
    }

    private void Overlay_Click(object sender, MouseButtonEventArgs e)
        => SettingsOverlay.Visibility = Visibility.Collapsed;

    private void Card_Click(object sender, MouseButtonEventArgs e) => e.Handled = true;

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) SettingsOverlay.Visibility = Visibility.Collapsed;
    }

    private void Opacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading) return;

        _data.Settings.OpacityPercent = Math.Round(e.NewValue);
        ApplyTheme(ThemePreset.Get(_data.Settings.Theme), _data.Settings.OpacityPercent);
        Raise(nameof(OpacityLabel));
        ScheduleSave();
    }

    private void Theme_Checked(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        if (sender is not RadioButton { DataContext: ThemePreset theme }) return;

        _data.Settings.Theme = theme.Id;
        ApplyTheme(theme, _data.Settings.OpacityPercent);
        ScheduleSave();
    }

    private void Topmost_Click(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _data.Settings.Topmost = TopmostCheck.IsChecked == true;
        Topmost = _data.Settings.Topmost;
        ScheduleSave();
    }

    private void AutoStart_Click(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        var on = AutoStartCheck.IsChecked == true;
        _data.Settings.AutoStart = on;
        SetAutoStart(on);
        ScheduleSave();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Flush();
        Application.Current.Shutdown();
    }

    private static bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(RunValueName) is string;
        }
        catch { return false; }
    }

    private static void SetAutoStart(bool on)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key is null) return;
            if (on) key.SetValue(RunValueName, $"\"{Environment.ProcessPath}\"");
            else key.DeleteValue(RunValueName, false);
        }
        catch { /* 权限不够就算了，不打断用户 */ }
    }

    // ---------- 窗口拖动 / 缩放 / 折叠 ----------

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed) return;
        try { DragMove(); } catch { /* 拖到一半松手，忽略 */ }
    }

    private void Grip_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_collapsed) return;

        var wa = SystemParameters.WorkArea;
        _expandWidth = Math.Clamp(Width + e.HorizontalChange, 240, Math.Max(260, wa.Width - 40));
        _expandHeight = Math.Clamp(Height + e.VerticalChange, 160, Math.Max(220, wa.Height - 40));
        Width = _expandWidth;
        Height = _expandHeight;
        ScheduleSave();
    }

    private void Collapse_Click(object sender, RoutedEventArgs e) => Collapse();

    private void CollapseSilently()
    {
        _collapsed = true;
        _expandWidth = Width;
        _expandHeight = Height;

        Shell.Visibility = Visibility.Collapsed;
        SettingsOverlay.Visibility = Visibility.Collapsed;
        MiniShell.Visibility = Visibility.Visible;

        var right = Left + Width;
        Width = 54;
        Height = 54;
        Left = right - 54;
    }

    private void Collapse()
    {
        CollapseSilently();
        _data.Settings.Collapsed = true;
        ScheduleSave();
    }

    private void Expand_Click(object sender, MouseButtonEventArgs e)
    {
        var right = Left + Width;

        _collapsed = false;
        MiniShell.Visibility = Visibility.Collapsed;
        Shell.Visibility = Visibility.Visible;

        Width = _expandWidth;
        Height = _expandHeight;
        Left = Math.Max(SystemParameters.WorkArea.Left + 8, right - Width);

        _data.Settings.Collapsed = false;
        ScheduleSave();
    }

    // ---------- 持久化 ----------

    private void ScheduleSave()
    {
        if (_loading) return;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void Flush()
    {
        var s = _data.Settings;
        s.Left = Left;
        s.Top = Top;
        s.Width = _expandWidth;
        s.Height = _expandHeight;
        s.Collapsed = _collapsed;
        s.Topmost = Topmost;

        _data.Notes = _notes.ToList();
        Store.Save(_data);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _saveTimer.Stop();
        Flush();
        base.OnClosing(e);
    }
}
