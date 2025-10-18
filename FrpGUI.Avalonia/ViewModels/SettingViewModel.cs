using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrpGUI.Avalonia.DataProviders;
using FrpGUI.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using FrpGUI.Enums;
using FzLib.Application.Startup;
using FzLib.Avalonia.Dialogs;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace FrpGUI.Avalonia.ViewModels
{
    public partial class SettingViewModel : ViewModelBase
    {
        private readonly IStartupManager startupManager;

        [ObservableProperty]
        private string newToken;

        [ObservableProperty]
        private string oldToken;

        [ObservableProperty]
        private ObservableCollection<ProcessInfo> processes;

        [ObservableProperty]
        private string serverAddress;

        [ObservableProperty]
        private bool startup;

        [ObservableProperty]
        private string token;
        
        [ObservableProperty]
        private string frpPath;

        public SettingViewModel(IDataProvider provider, IDialogService dialogService, IStartupManager startupManager,
            UIConfig config) : base(provider, dialogService)
        {
            this.startupManager = startupManager;
            if (!OperatingSystem.IsBrowser())
            {
                startup = startupManager.IsStartupEnabled();
            }

            Config = config;
            ServerAddress = config.ServerAddress;
            FrpPath = config.FrpPath;
            FillProcesses();
            Config.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Config.RunningMode) && Config.RunningMode == RunningMode.Service)
                {
                    Startup = false;
                }
            };
        }

        public UIConfig Config { get; }

        private async void FillProcesses()
        {
            try
            {
                Processes = new ObservableCollection<ProcessInfo>(await DataProvider.GetSystemProcesses());
            }
            catch (Exception ex)
            {
            }
        }

        [RelayCommand]
        private async Task KillProcessAsync(ProcessInfo p)
        {
            Debug.Assert(p != null);
            try
            {
                await DataProvider.KillProcess(p.Id);
                Processes.Remove(p);
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorDialogAsync("结束进程失败", ex);
            }
        }

        partial void OnStartupChanged(bool value)
        {
            if (!OperatingSystem.IsBrowser())
            {
                if (value)
                {
                    startupManager.EnableStartup("s");
                    Config.ShowTrayIcon = true;
                }
                else
                {
                    startupManager.DisableStartup();
                }
            }
        }

        public async Task<bool> TryCloseAsync()
        {
            Config.ServerAddress = ServerAddress;
            Config.FrpPath = FrpPath;
            if (!string.IsNullOrEmpty(Token)) //如果密码修改了
            {
                Config.ServerToken = Token;
            }

            Config.Save();

            return Config.RunningMode != RunningMode.Service || await CheckServerAsync();
        }

        [RelayCommand]
        private async Task SwitchRunningModeAsync()
        {
            Config.RunningMode = Config.RunningMode == RunningMode.Singleton
                ? RunningMode.Service
                : RunningMode.Singleton;
            Config.Save();
            if (OperatingSystem.IsBrowser())
            {
                JsInterop.Reload();
            }
            else
            {
                string exePath = Environment.ProcessPath;
                Process.Start(new ProcessStartInfo(exePath)
                {
                    UseShellExecute = true
                });
                await (App.Current as App).ShutdownAsync();
            }
        }

        private async Task<bool> CheckServerAsync()
        {
            WebDataProvider provider = new WebDataProvider(Config);
            try
            {
                var result = await provider.VerifyTokenAsync();
                switch (result)
                {
                    case TokenVerification.OK:
                        return true;
                    case TokenVerification.NotEqual:
                        await DialogService.ShowErrorDialogAsync("错误", "密码错误");
                        return false;
                    case TokenVerification.NeedSet:
                        await DialogService.ShowErrorDialogAsync("错误", "请先设置服务端密码");
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorDialogAsync("错误", "无法连接到服务器：" + ex.Message);
                return false;
            }
        }

        [RelayCommand]
        private async Task SetTokenAsync()
        {
            try
            {
                Config.ServerAddress = ServerAddress;
                await DataProvider.SetTokenAsync(OldToken, NewToken);
                Config.ServerToken = NewToken;
                Config.Save();

                await DialogService.ShowOkDialogAsync("修改密码", "修改密码成功");
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorDialogAsync("修改密码失败", ex);
            }
        }
    }
}