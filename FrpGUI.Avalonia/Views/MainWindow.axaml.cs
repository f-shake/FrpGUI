using Avalonia.Controls;
using Avalonia.Interactivity;
using FrpGUI.Avalonia.ViewModels;
using FrpGUI.Enums;
using FrpGUI.Models;
using FzLib.Avalonia.Controls;
using FzLib.Avalonia.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FrpGUI.Avalonia.Views;

public partial class MainWindow : ExtendedWindow
{
    private readonly UIConfig config = App.Services.GetRequiredService<UIConfig>();
    private readonly IDialogService dialogService;
    private readonly FrpProcessCollection processes = App.Services.GetService<FrpProcessCollection>();

    private bool forceClose = false;

    public MainWindow()
    {
        Trace.WriteLine("正在通过默认构造函数创建MainWindow，仅用于调试");
    }

    public MainWindow(IDialogService dialogService, MainView mainView)
    {
        this.dialogService = dialogService;
        InitializeComponent();

        grid.Children.Add(mainView);
        if (OperatingSystem.IsWindows())
        {
            grid.Children.Add(new WindowButtons());
        }
    }

    public async Task TryCloseAsync()
    {
        if (config.RunningMode == RunningMode.Service)
        {
            forceClose = true;
            Close();
        }

        if (processes != null && processes.Any(p => p.Value.ProcessStatus == ProcessStatus.Running))
        {
            if (!IsVisible)
            {
                Show();
            }

            var runningFrps = processes.Where(p => p.Value.ProcessStatus == ProcessStatus.Running).ToList();
            if (await dialogService.ShowYesNoDialogAsync("退出", $"存在{runningFrps.Count}个正在运行的frp进程，是否退出？") == true)
            {
                foreach (var frp in runningFrps)
                {
                    await frp.Value.StopAsync();
                }

                forceClose = true;
                Close();
            }
        }
        else
        {
            forceClose = true;
            Close();
        }
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (forceClose)
        {
            return;
        }

        if (config.ShowTrayIcon)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            await TryCloseAsync();
        }
    }
}