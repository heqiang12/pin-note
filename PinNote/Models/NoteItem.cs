using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PinNote.Models;

public enum NoteStatus
{
    Todo = 0,
    Doing = 1,
    Done = 2
}

/// <summary>一条便签记录。</summary>
public class NoteItem : INotifyPropertyChanged
{
    private string _text = string.Empty;
    private NoteStatus _status = NoteStatus.Todo;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;
            _text = value;
            OnPropertyChanged();
        }
    }

    public NoteStatus Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusTip));
            OnPropertyChanged(nameof(IsDone));
        }
    }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsDone => _status == NoteStatus.Done;

    public string StatusTip => _status switch
    {
        NoteStatus.Todo => "待做 · 点一下变进行中",
        NoteStatus.Doing => "进行中 · 点一下变已完成",
        _ => "已完成 · 点一下回到待做"
    };

    /// <summary>列表里显示的时间：今天只显示时分，往前逐级加日期。</summary>
    public string TimeText
    {
        get
        {
            var d = CreatedAt;
            var today = DateTime.Today;
            if (d.Date == today) return d.ToString("HH:mm");
            if (d.Date == today.AddDays(-1)) return "昨天 " + d.ToString("HH:mm");
            if (d.Year == today.Year) return d.ToString("MM-dd HH:mm");
            return d.ToString("yyyy-MM-dd HH:mm");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
