using FrpGUI.Configs;
using FrpGUI.Enums;
using FrpGUI.Models;
using FrpGUI.Services;
using FrpGUI.WebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrpGUI.WebAPI.Controllers;

[NeedToken]
[ApiController]
[Route("[controller]")]
public class ProcessController : FrpControllerBase
{
    private readonly FrpProcessCollection processes;
    private readonly WebConfigService serverConfigService;

    public ProcessController(AppConfig config, LoggerBase logger, FrpProcessCollection processes, WebConfigService serverConfigService)
        : base(config, logger)
    {
        this.processes = processes;
        this.serverConfigService = serverConfigService;
    }

    [HttpGet("Status")]
    public IList<IFrpProcess> GetFrpProcessList()
    {
        return processes.GetAll();
    }

    [HttpPost("Start/{id}")]
    public Task StartAsync(string id)
    {
        var frp = processes.GetOrCreateProcess(id);
        if (frp.Config is ClientConfig)
        {
            serverConfigService.ThrowIfServerOnly();
        }
        logger.Info($"指令：启动", frp.Config);
        return frp.StartAsync();
    }

    [HttpPost("Stop/{id}")]
    public Task StopAsync(string id)
    {
        var frp = processes.GetOrCreateProcess(id);
        logger.Info($"指令：停止", frp.Config);
        return frp.StopAsync();
    }

    [HttpPost("Restart/{id}")]
    public Task RestartAsync(string id)
    {
        var frp = processes.GetOrCreateProcess(id);
        logger.Info($"指令：重启", frp.Config);
        return frp.RestartAsync();
    }

    [HttpGet("Status/{id}")]
    public ProcessStatus GetStatusAsync(string id)
    {
        return processes.GetOrCreateProcess(id).ProcessStatus;
    }

    [HttpGet("All")]
    public List<ProcessInfo> GetSystemFrpProcesses()
    {
        return ProcessInfo.GetFrpProcesses();
    }

    [HttpPost("Kill/{id}")]
    public void KillProcess(int id)
    {
        ProcessInfo.KillProcess(id);
    }
}