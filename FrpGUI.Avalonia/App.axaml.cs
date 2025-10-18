using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using FrpGUI.Avalonia.DataProviders;
using FrpGUI.Avalonia.Factories;
using FrpGUI.Avalonia.ViewModels;
using FrpGUI.Avalonia.Views;
using FrpGUI.Configs;
using FrpGUI.Enums;
using FrpGUI.Models;
using FrpGUI.Services;
using FzLib.Application.Startup;
using FzLib.Avalonia.Dialogs;
using FzLib.Avalonia.Services;
using FzLib.Programming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace FrpGUI.Avalonia;

public partial class App : Application
{
    private MainWindow mainWindow;

    private bool dontOpen = false;

    public App()
    {
    }

    public static IServiceProvider Services { get; private set; }

    public IHost AppHost { get; private set; }

    public static void AddViewAndViewModel<TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TViewModel>(
        HostApplicationBuilder builder, ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TView : Control, new()
        where TViewModel : class
    {
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                builder.Services.AddSingleton<TViewModel>();
                builder.Services.AddSingleton(s => new TView()
                    { DataContext = s.GetRequiredService<TViewModel>() });

                break;
            case ServiceLifetime.Transient:
                builder.Services.AddTransient<TViewModel>();
                builder.Services.AddTransient(s => new TView()
                    { DataContext = s.GetRequiredService<TViewModel>() });

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (!TcpSingleInstanceHelper.EnsureSingleInstance(() =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        if (desktop.MainWindow == null)
                        {
                            desktop.MainWindow = mainWindow;
                        }

                        mainWindow.BringToFront();
                    });
                    return Task.CompletedTask;
                }))
            {
                Log.Information("检测到已有实例在运行，程序将退出");
                dontOpen = true;
                desktop.Shutdown();
                Environment.Exit(0);
                return;
            }
        }

        //Windows上使用微软雅黑
        if (OperatingSystem.IsWindows())
        {
            Resources.Add("ContentControlThemeFontFamily", new FontFamily("Microsoft YaHei UI"));
        }

        //浏览器端需要设置内置字体才可正常显示中文
        else if (OperatingSystem.IsBrowser())
        {
            Resources.Add("ContentControlThemeFontFamily",
                new FontFamily("avares://FrpGUI.Avalonia/Assets#Microsoft YaHei"));
        }

        var builder = Host.CreateApplicationBuilder();
        var uiconfig = UIConfig.Get();

        //浏览器一定是使用服务模式而不是单机模式
        if (OperatingSystem.IsBrowser())
        {
            if (uiconfig.RunningMode == RunningMode.Singleton)
            {
                uiconfig.RunningMode = RunningMode.Service;
            }
        }

        builder.Services.AddDialogService();
        builder.Services.AddClipboardService();
        builder.Services.AddStorageProviderService();
        builder.Services.AddProgressOverlayService();

        switch (uiconfig.RunningMode)
        {
            case RunningMode.Singleton:
                builder.Services.AddSingleton<IDataProvider, LocalDataProvider>();
                builder.Services.AddHostedService<LocalAppLifetimeService>();
                builder.Services.AddSingleton<FrpProcessCollection>();
                builder.Services.AddSingleton(AppConfig.Get());
                break;

            case RunningMode.Service:
                builder.Services.AddSingleton<IDataProvider, WebDataProvider>();
                break;
        }

        var logger = new LocalLogger();
        builder.Services.AddSingleton<LoggerBase>(logger);
        builder.Services.AddSingleton<LocalLogger>(logger);

        if (!OperatingSystem.IsBrowser())
        {
            builder.Services.AddStartupManager();
        }

        builder.Services.AddTransient<MainWindow>();

        builder.Services.AddTransient<ClientPanel>();
        builder.Services.AddTransient<ServerPanel>();
        builder.Services.AddTransient<FrpConfigViewModel>();

        builder.Services.AddSingleton<DialogFactory>();

        AddViewAndViewModel<MainView, MainViewModel>(builder);
        AddViewAndViewModel<RuleDialog, RuleViewModel>(builder);
        AddViewAndViewModel<SettingsDialog, SettingViewModel>(builder);
        builder.Services.AddTransient<LogPanel>();
        builder.Services.AddTransient<LogViewModel>();

        builder.Services.AddSingleton(uiconfig);
        builder.Services.AddSingleton<IAppConfig, UIConfig>(s => uiconfig);

        AppHost = builder.Build();

        Services = AppHost.Services;
        AppHost.Start();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (dontOpen)
        {
            return;
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            //BindingPlugins.DataValidators.RemoveAt(0);
            mainWindow = Services.GetRequiredService<MainWindow>();
            var startup = desktop.Args is { Length: > 0 } && desktop.Args[0] == "s";
            if (!startup)
            {
                desktop.MainWindow = mainWindow;
            }

            desktop.Exit += Desktop_Exit;

            InitializeTrayIcon(startup);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime s)
        {
            s.MainView = new MainView();
        }

        base.OnFrameworkInitializationCompleted();
    }

    public async Task ShutdownAsync()
    {
        await AppHost.StopAsync();
        Environment.Exit(0);
    }

    private async void Desktop_Exit(object sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        TrayIcon.GetIcons(this)[0].Dispose();
        await AppHost.StopAsync();
    }

    private async void ExitMenuItem_Click(object sender, EventArgs e)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            throw new PlatformNotSupportedException();
        }

        var mainWindow = desktop.MainWindow as MainWindow;
        await mainWindow.TryCloseAsync();
    }

    private void InitializeTrayIcon(bool force)
    {
        Services.GetRequiredService<UIConfig>().PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(UIConfig.ShowTrayIcon))
            {
                TrayIcon.GetIcons(this)[0].IsVisible = Services.GetRequiredService<UIConfig>().ShowTrayIcon;
            }
        };
        TrayIcon.GetIcons(this)[0].IsVisible = force || Services.GetRequiredService<UIConfig>().ShowTrayIcon;
    }

    private void TrayIcon_Clicked(object sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow == null)
            {
                desktop.MainWindow = mainWindow;
            }

            if (desktop.MainWindow.IsVisible)
            {
                desktop.MainWindow.Hide();
            }
            else
            {
                desktop.MainWindow.Show();
            }
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
    }
}