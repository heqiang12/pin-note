using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using PinNote.Models;

namespace PinNote.Services;

/// <summary>数据落盘：%APPDATA%\PinNote\data.json。</summary>
public static class Store
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Dir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PinNote");

    public static string FilePath => Path.Combine(Dir, "data.json");

    public static AppData Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new AppData();
            var json = File.ReadAllText(FilePath);
            var data = JsonSerializer.Deserialize<AppData>(json, Options);
            if (data is null) return new AppData();
            data.Settings ??= new AppSettings();
            data.Notes ??= new List<NoteItem>();
            return data;
        }
        catch
        {
            // 数据坏了也不能让程序起不来：留个备份，从空白开始
            try
            {
                var bak = FilePath + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                File.Copy(FilePath, bak, true);
            }
            catch { /* 备份失败就算了 */ }
            return new AppData();
        }
    }

    public static void Save(AppData data)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var json = JsonSerializer.Serialize(data, Options);
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, json);
            // 先写临时文件再替换，避免写一半断电把数据写没
            if (File.Exists(FilePath)) File.Replace(tmp, FilePath, null);
            else File.Move(tmp, FilePath);
        }
        catch { /* 存不上就别打扰用户 */ }
    }
}
