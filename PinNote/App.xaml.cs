using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace PinNote;

public partial class App : Application
{
    private const string WindowTitle = "PinNote";
    private Mutex? _instanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 只允许跑一个：已经在运行就把它叫到前面
        _instanceMutex = new Mutex(true, "PinNote.SingleInstance.Mutex", out bool isFirst);
        if (!isFirst)
        {
            var hwnd = Native.FindWindow(null, WindowTitle);
            if (hwnd != IntPtr.Zero)
            {
                Native.ShowWindow(hwnd, 9 /*SW_RESTORE*/);
                Native.SetForegroundWindow(hwnd);
            }
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnUnhandled;

        var window = new MainWindow { Title = WindowTitle };
        window.Show();
    }

    private void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(Services.Store.Dir);
            var log = Path.Combine(Services.Store.Dir, "error.log");
            File.AppendAllText(log, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.Exception}\r\n\r\n");
        }
        catch { /* 日志失败也不影响继续跑 */ }

        e.Handled = true;
        MessageBox.Show("出了点小问题，已记录到日志：\n" + e.Exception.Message,
            "PinNote", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}

internal static class Native
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
