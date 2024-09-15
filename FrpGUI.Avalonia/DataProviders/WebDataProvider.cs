﻿using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FrpGUI.Configs;
using FrpGUI.Models;
using System.Dynamic;
using System.Text.Json.Nodes;
using FrpGUI.Enums;
using System.Net;
using System.Linq;
using FrpGUI.Avalonia.ViewModels;

namespace FrpGUI.Avalonia.DataProviders
{
    public class WebDataProvider(UIConfig config) : HttpRequester(config), IDataProvider
    {
        private const string AddClientEndpoint = "Config/FrpConfigs/Add/Client";
        private const string AddServerEndpoint = "Config/FrpConfigs/Add/Server";
        private const string DeleteFrpConfigsEndpoint = "Config/FrpConfigs/Delete";
        private const string FrpConfigsEndpoint = "Config/FrpConfigs";
        private const string FrpStatusEndpoint = "Process/Status";
        private const string LogsEndpoint = "Log/List";
        private const string ModifyConfigEndpoint = "Config/FrpConfigs/Modify";
        private const string RestartFrpEndpoint = "Process/Restart";
        private const string StartFrpEndpoint = "Process/Start";
        private const string StopFrpEndpoint = "Process/Stop";
        private const string TokenEndpoint = "Token";
        private readonly UIConfig config = config;

        public Task<TokenVerification> VerifyTokenAsync()
        {
            return GetObjectAsync<TokenVerification>(TokenEndpoint);
        }

        public Task SetTokenAsync(string oldToken, string newToken)
        {
            return PostAsync<TokenVerification>($"{TokenEndpoint}?oldToken={WebUtility.UrlEncode(oldToken)}&newToken={WebUtility.UrlEncode(newToken)}");
        }

        public Task<ClientConfig> AddClientAsync()
        {
            return PostAsync<ClientConfig>(AddClientEndpoint);
        }

        public Task<ServerConfig> AddServerAsync()
        {
            return PostAsync<ServerConfig>(AddServerEndpoint);
        }

        public Task DeleteFrpConfigAsync(string id)
        {
            return PostAsync($"{DeleteFrpConfigsEndpoint}/{id}");
        }


        public Task<List<FrpConfigBase>> GetFrpConfigsAsync()
        {
            return GetObjectAsync<List<FrpConfigBase>>(FrpConfigsEndpoint);
        }

        public Task<FrpStatusInfo> GetFrpStatusAsync(string id)
        {
            return PostAsync<FrpStatusInfo>($"{FrpStatusEndpoint}/{id}");
        }

        public async Task<IList<FrpStatusInfo>> GetFrpStatusesAsync()
        {
            return await GetObjectAsync<IList<FrpStatusInfo>>(FrpStatusEndpoint);
        }

        public Task<List<LogEntity>> GetLogsAsync(DateTime timeAfter)
        {
            return GetObjectAsync<List<LogEntity>>(LogsEndpoint, ("timeAfter", timeAfter.ToString("o")));
        }

        public Task ModifyConfigAsync(FrpConfigBase config)
        {
            return PostAsync(ModifyConfigEndpoint, config);
        }

        public Task RestartFrpAsync(string id)
        {
            return PostAsync($"{RestartFrpEndpoint}/{id}");
        }

        public Task StartFrpAsync(string id)
        {
            return PostAsync($"{StartFrpEndpoint}/{id}");
        }

        public Task StopFrpAsync(string id)
        {
            return PostAsync($"{StopFrpEndpoint}/{id}");
        }


    }
}
