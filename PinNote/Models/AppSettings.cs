namespace PinNote.Models;

/// <summary>窗口与外观设置，全部存进 data.json。</summary>
public class AppSettings
{
    /// <summary>背景不透明度百分比（20-100），只影响底色，不影响文字。</summary>
    public double OpacityPercent { get; set; } = 88;

    /// <summary>配色方案 Id，见 ThemePreset.All。</summary>
    public string Theme { get; set; } = "graphite";

    public double Width { get; set; } = 320;
    public double Height { get; set; } = 440;

    /// <summary>窗口左上角坐标；-1 表示还没定位过，启动时自动贴右上角。</summary>
    public double Left { get; set; } = -1;
    public double Top { get; set; } = -1;

    public bool Topmost { get; set; } = true;

    /// <summary>是否折叠成小方块。</summary>
    public bool Collapsed { get; set; }

    /// <summary>开机自启（写 HKCU Run）。</summary>
    public bool AutoStart { get; set; }
}

public class AppData
{
    public List<NoteItem> Notes { get; set; } = new();
    public AppSettings Settings { get; set; } = new();
}
