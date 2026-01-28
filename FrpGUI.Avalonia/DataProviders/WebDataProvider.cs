using FrpGUI.Avalonia.Models;
using FrpGUI.Avalonia.ViewModels;
using FrpGUI.Enums;
using FrpGUI.Models;
using FzLib.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FrpGUI.Avalonia.DataProviders
{
    public class WebDataProvider : HttpRequester, IDataProvider
    {
        private const string AuthorizationKey = "Authorization";

        private const string AddClientEndpoint = "Config/FrpConfigs/Add/Client";
        private const string AddServerEndpoint = "Config/FrpConfigs/Add/Server";
        private const string DeleteFrpConfigsEndpoint = "Config/FrpConfigs/Delete";
        private const string ConfigsEndpoint = "Config/Configs";
        private const string FrpStatusEndpoint = "Process/Status";
        private const string KillProcessEndpoint = "Process/Kill";
        private const string LogsEndpoint = "Log/List";
        private const string ModifyConfigEndpoint = "Config/FrpConfigs/Modify";
        private const string RestartFrpEndpoint = "Process/Restart";
        private const string StartFrpEndpoint = "Process/Start";
        private const string StopFrpEndpoint = "Process/Stop";
        private const string SystemProcessesEndpoint = "Process/All";
        private const string TokenEndpoint = "Token";
        private readonly UIConfig config;
        private readonly LocalLogger logger;
        private PeriodicTimer timer;


        private void WriteAuthorizationHeader()
        {
            if (string.IsNullOrWhiteSpace(config.ServerToken))
            {
                return;
            }

            if (httpClient.DefaultRequestHeaders.TryGetValues(AuthorizationKey, out IEnumerable<string> values))
            {
                var count = values.Count();
                if (count >= 1)
                {
                    if (values.First() == config.ServerToken)
                    {
                        return;
                    }

                    httpClient.DefaultRequestHeaders.Remove(AuthorizationKey);
                    httpClient.DefaultRequestHeaders.Add(AuthorizationKey, config.ServerToken);
                }
                else
                {
                    throw new Exception();
                }
            }
            else
            {
                httpClient.DefaultRequestHeaders.Add(AuthorizationKey, config.ServerToken);
            }
        }

        private List<(string Name, Func<Task> task)> timerTasks = new List<(string Name, Func<Task> task)>();

        public WebDataProvider(UIConfig config, LocalLogger logger)
        {
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            this.config = config;
            this.logger = logger;
            StartTimer();
        }

        protected override string BaseUrl => config.ServerAddress;

        protected override void OnSending()
        {
            WriteAuthorizationHeader();
        }

        public WebDataProvider(UIConfig config)
        {
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            this.config = config;
        }

        public Task<ClientConfig> AddClientAsync()
        {
            return PostAsync(AddClientEndpoint, JContext.ClientConfig);
        }

        public Task<ServerConfig> AddServerAsync()
        {
            return PostAsync(AddServerEndpoint, JContext.ServerConfig);
        }

        public void AddTimerTask(string name, Func<Task> task)
        {
            ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
            ArgumentNullException.ThrowIfNull(task, nameof(task));
            timerTasks.Add((name, task));
        }

        public Task DeleteFrpConfigAsync(string id)
        {
            return PostAsync($"{DeleteFrpConfigsEndpoint}/{id}");
        }

        public Task<List<FrpConfigBase>> GetConfigsAsync()
        {
            return GetObjectAsync(ConfigsEndpoint, JContext.ListFrpConfigBase);
        }

        public Task<FrpStatusInfo> GetFrpStatusAsync(string id)
        {
            return PostAsync($"{FrpStatusEndpoint}/{id}", JContext.FrpStatusInfo);
        }

        public async Task<List<FrpStatusInfo>> GetFrpStatusesAsync()
        {
            var result = await GetObjectAsync(FrpStatusEndpoint, JContext.ListFrpStatusInfo);
            return result; //.Select(p => new FrpStatusInfo(p)).ToList();
        }

        private FrpAvaloniaSourceGenerationContext JContext => FrpAvaloniaSourceGenerationContext.Default;

        public Task<List<LogEntity>> GetLogsAsync(DateTime timeAfter)
        {
            return GetObjectAsync(LogsEndpoint, JContext.ListLogEntity,
                [("timeAfter", timeAfter.ToString("yyyy-MM-ddTHH:mm:ss.fffffff"))]);
        }

        public Task<List<ProcessInfo>> GetSystemProcesses()
        {
            return GetObjectAsync(SystemProcessesEndpoint, JContext.ListProcessInfo);
        }

        public Task KillProcess(int id)
        {
            return PostAsync($"{KillProcessEndpoint}/{id}");
        }

        public Task ModifyConfigAsync(FrpConfigBase config)
        {
            switch (config)
            {
                case ClientConfig c:
                    return PostAsync(ModifyConfigEndpoint, config, JContext.ClientConfig);
                case ServerConfig s:
                    return PostAsync(ModifyConfigEndpoint, config, JContext.ServerConfig);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Task RestartFrpAsync(string id)
        {
            return PostAsync($"{RestartFrpEndpoint}/{id}");
        }

        public Task SetTokenAsync(string oldToken, string newToken)
        {
            return PostAsync(
                $"{TokenEndpoint}?oldToken={WebUtility.UrlEncode(oldToken ?? "")}&newToken={WebUtility.UrlEncode(newToken)}",
                JContext.TokenVerification);
        }

        public Task StartFrpAsync(string id)
        {
            return PostAsync($"{StartFrpEndpoint}/{id}");
        }

        public Task StopFrpAsync(string id)
        {
            return PostAsync($"{StopFrpEndpoint}/{id}");
        }

        public Task<TokenVerification> VerifyTokenAsync()
        {
            return GetObjectAsync(TokenEndpoint, JContext.TokenVerification);
        }

        private async void StartTimer()
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
            while (await timer.WaitForNextTickAsync())
            {
                foreach (var (name, task) in timerTasks.ToList())
                {
                    try
                    {
                        await task.Invoke();
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"执行定时任务“{name}”失败", null, ex);
                    }
                }
            }
        }
    }
}