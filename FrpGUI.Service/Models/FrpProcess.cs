using FrpGUI.Enums;
using FrpGUI.Services;
using System.Text.Json.Serialization;

namespace FrpGUI.Models;

public class FrpProcess : IFrpProcess
{
    private readonly LoggerBase logger;

    public FrpProcess()
    {
    }

    public FrpProcess(FrpConfigBase config, LoggerBase logger, IAppConfig appConfig)
    {
        Config = config;
        this.logger = logger;
        Process = new ProcessService(Config, logger, appConfig);
        Process.Exited += Process_Exited;
    }

    public event EventHandler StatusChanged;

    public FrpConfigBase Config { get; }

    [JsonIgnore]
    public ProcessService Process { get; protected set; }

    public ProcessStatus ProcessStatus { get; set; }

    public void ChangeStatus(ProcessStatus status)
    {
        logger.Info("进程状态改变：" + status.ToString(), Config);
        ProcessStatus = status;
        StatusChanged?.Invoke(this, new EventArgs());
    }

    public async Task RestartAsync()
    {
        if (ProcessStatus == ProcessStatus.Stopped)
        {
            throw new Exception("进程未在运行");
        }

        ChangeStatus(ProcessStatus.Busy);
        try
        {
            await Process.RestartAsync();
        }
        catch (Exception ex)
        {
            ChangeStatus(ProcessStatus.Stopped);
            throw;
        }

        ChangeStatus(ProcessStatus.Running);
    }

    public async Task StartAsync()
    {
        if (ProcessStatus == ProcessStatus.Running)
        {
            throw new Exception("进程已在运行");
        }

        ChangeStatus(ProcessStatus.Busy);
        try
        {
            await Task.Run(Process.Start).ConfigureAwait(false);
            ChangeStatus(ProcessStatus.Running);
        }
        catch (Exception ex)
        {
            ChangeStatus(ProcessStatus.Stopped);
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (ProcessStatus == ProcessStatus.Stopped)
        {
            throw new Exception("进程未在运行");
        }

        ChangeStatus(ProcessStatus.Busy);
        await Process.StopAsync();
        ChangeStatus(ProcessStatus.Stopped);
    }

    private void Process_Exited(object sender, EventArgs e)
    {
        ChangeStatus(ProcessStatus.Stopped);
    }
}