﻿using FrpGUI.Configs;
using FrpGUI.Models;
using FrpGUI.Services;
using FrpGUI.WebAPI.Services;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace FrpGUI.WebAPI.Controllers;

[NeedToken]
[ApiController]
[Route("[controller]")]
public class ConfigController : FrpControllerBase
{
    private readonly FrpProcessCollection processes;
    private readonly WebConfigService webConfigService;

    public ConfigController(AppConfig configs, LoggerBase logger, FrpProcessCollection processes, WebConfigService webConfigService)
        : base(configs, logger)
    {
        this.processes = processes;
        this.webConfigService = webConfigService;
    }

    [HttpPost("FrpConfigs/Add/Client")]
    public ClientConfig AddClientConfig()
    {
        webConfigService.ThrowIfServerOnly();
        logger.Info($"指令：新增客户端");
        ClientConfig clientConfig = new ClientConfig();
        configs.FrpConfigs.Add(clientConfig);
        configs.Save();
        return clientConfig;
    }

    [HttpPost("FrpConfigs/Add/Server")]
    public ServerConfig AddServerConfig()
    {
        logger.Info($"指令：新增服务端");
        ServerConfig serverConfig = new ServerConfig();
        configs.FrpConfigs.Add(serverConfig);
        configs.Save();
        return serverConfig;
    }

    [HttpPost("FrpConfigs/Delete/{id}")]
    public async Task DeleteFrpConfigAsync(string id)
    {
        var frp = await processes.RemoveFrpAsync(id);
        logger.Info($"指令：删除配置", frp);
    }

    [HttpGet("Configs")]
    public List<FrpConfigBase> GetConfigList()
    {
        return configs.FrpConfigs;
    }

    [HttpPost("FrpConfigs/Modify")]
    public void ModifyConfig(FrpConfigBase config)
    {
        var p = processes.GetOrCreateProcess(config.ID);
        logger.Info($"指令：应用配置", p.Config);
        if (p.Config.GetType() != config.GetType())
        {
            throw new StatusBasedException("提供的配置与已有配置类型不同", System.Net.HttpStatusCode.BadRequest);
        }
        //需要指定实际的类型，不然只会Adapt基类属性
        if (config is ClientConfig c)
        {
            c.Adapt(p.Config as ClientConfig);
        }
        else if (config is ServerConfig s)
        {
            s.Adapt(p.Config as ServerConfig);
        }
        configs.Save();
    }
}