﻿using Avalonia;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

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
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/logs.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        Log.Information("程序启动");
#if !DEBUG
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        try
        {
#endif
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
#if !DEBUG
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "未捕获的主线程错误");
        }
        finally
        {
            Log.CloseAndFlush();
        }
#endif
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.ExceptionObject as Exception, "未捕获的AppDomain异常");
    }

    private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "未捕获的TaskScheduler异常");
    }
}
