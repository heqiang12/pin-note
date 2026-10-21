using System.ComponentModel;
using System.Windows.Media;

namespace PinNote.Models;

/// <summary>一套配色。文字色跟着底色走，浅底用深字。</summary>
public class ThemePreset : INotifyPropertyChanged
{
    private bool _isSelected;

    public ThemePreset(string id, string name, Color background, Color text, Color sub, bool isDark)
    {
        Id = id;
        Name = name;
        Background = background;
        Text = text;
        Sub = sub;
        IsDark = isDark;
        Swatch = Freeze(new SolidColorBrush(background));
    }

    public string Id { get; }
    public string Name { get; }
    public Color Background { get; }
    public Color Text { get; }
    public Color Sub { get; }
    public bool IsDark { get; }

    /// <summary>色板上的小圆点。</summary>
    public Brush Swatch { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public Color Border => IsDark
        ? Color.FromArgb(38, 255, 255, 255)
        : Color.FromArgb(30, 0, 0, 0);

    public Color Hover => IsDark
        ? Color.FromArgb(20, 255, 255, 255)
        : Color.FromArgb(16, 0, 0, 0);

    public Color Field => IsDark
        ? Color.FromArgb(24, 255, 255, 255)
        : Color.FromArgb(20, 0, 0, 0);

    public event PropertyChangedEventHandler? PropertyChanged;

    private static SolidColorBrush Freeze(SolidColorBrush brush)
    {
        brush.Freeze();
        return brush;
    }

    public static readonly IReadOnlyList<ThemePreset> All = new[]
    {
        new ThemePreset("graphite", "石墨", Color.FromRgb(0x14, 0x16, 0x1C), Color.FromRgb(0xE8, 0xEA, 0xEE), Color.FromRgb(0x7C, 0x82, 0x8E), true),
        new ThemePreset("midnight", "墨黑", Color.FromRgb(0x0A, 0x0B, 0x0F), Color.FromRgb(0xE6, 0xE8, 0xEC), Color.FromRgb(0x71, 0x76, 0x82), true),
        new ThemePreset("ink",      "靛蓝", Color.FromRgb(0x1B, 0x23, 0x33), Color.FromRgb(0xE4, 0xEA, 0xF4), Color.FromRgb(0x7E, 0x8B, 0xA4), true),
        new ThemePreset("forest",   "松绿", Color.FromRgb(0x16, 0x22, 0x1C), Color.FromRgb(0xE2, 0xED, 0xE6), Color.FromRgb(0x76, 0x8C, 0x7F), true),
        new ThemePreset("cream",    "奶油", Color.FromRgb(0xF7, 0xF1, 0xE3), Color.FromRgb(0x2A, 0x26, 0x20), Color.FromRgb(0x8A, 0x80, 0x6E), false),
        new ThemePreset("paper",    "米白", Color.FromRgb(0xFB, 0xFA, 0xF7), Color.FromRgb(0x22, 0x20, 0x1C), Color.FromRgb(0x88, 0x84, 0x7C), false),
        new ThemePreset("sky",      "淡蓝", Color.FromRgb(0xDC, 0xE9, 0xF7), Color.FromRgb(0x17, 0x22, 0x2E), Color.FromRgb(0x69, 0x7A, 0x8D), false),
        new ThemePreset("mint",     "薄荷", Color.FromRgb(0xD9, 0xED, 0xE1), Color.FromRgb(0x16, 0x25, 0x1C), Color.FromRgb(0x6B, 0x83, 0x72), false),
        new ThemePreset("rose",     "藕粉", Color.FromRgb(0xF5, 0xDF, 0xDF), Color.FromRgb(0x2C, 0x1E, 0x1E), Color.FromRgb(0x8C, 0x72, 0x72), false),
    };

    public static ThemePreset Get(string? id)
        => All.FirstOrDefault(t => t.Id == id) ?? All[0];
}
