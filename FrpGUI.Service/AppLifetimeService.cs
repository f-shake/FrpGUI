﻿using FrpGUI.Configs;
using FrpGUI.Service.Models;

namespace FrpGUI.Service
{
    public class AppLifetimeService(AppConfig config, Logger logger, FrpProcessService processes) : IHostedService
    {

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var frpConfig in processes.Keys)
            {
                if (frpConfig.AutoStart)
                {
                    await processes.GetOrCreateProcess(frpConfig.ID).StartAsync();
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            config.Save();
            foreach (FrpProcess process in processes.Values)
            {
                if (process.ProcessStatus == Enums.ProcessStatus.Running)
                {
                    await process.StopAsync();
                }
            }
        }
    }
}
