using Avalonia;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using FzLib.Application;

namespace FrpGUI.Avalonia.Desktop;

class Program
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .LogToTrace()
            .UsePlatformDetect();
    //.UseDesktopWebView();

    [STAThread]
    public static void Main(string[] args)
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/logs.txt", rollingInterval: RollingInterval.Day)
            .WriteTo.Console()
            .CreateLogger();
        Log.Information("程序启动");

        UnhandledExceptionCatcher.WithCatcher(() => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args))
            .Catch((e, s) => { Log.Fatal(e, "未捕获的异常"); })
            .Finally(Log.CloseAndFlush)
            .Run();
    }
}