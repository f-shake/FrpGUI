using FrpGUI.Configs;
using FrpGUI.Enums;
using FrpGUI.Models;
using FrpGUI.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrpGUI.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class ProcessController : FrpControllerBase
{
    private readonly FrpProcessService processes;

    public ProcessController(AppConfig config, Logger logger, FrpProcessService processes) : base(config, logger)
    {
        this.processes = processes;
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
        logger.Info($"ָ�����", frp.Config);
        return frp.StartAsync();
    }

    [HttpPost("Stop/{id}")]
    public Task StopAsync(string id)
    {
        var frp = processes.GetOrCreateProcess(id);
        logger.Info($"ָ�ֹͣ", frp.Config);
        return frp.StopAsync();
    }

    [HttpPost("Restart/{id}")]
    public Task RestartAsync(string id)
    {
        var frp = processes.GetOrCreateProcess(id);
        logger.Info($"ָ�����", frp.Config);
        return frp.RestartAsync();
    }

    [HttpGet("Status/{id}")]
    public ProcessStatus GetStatusAsync(string id)
    {
        return processes.GetOrCreateProcess(id).ProcessStatus;
    }
}