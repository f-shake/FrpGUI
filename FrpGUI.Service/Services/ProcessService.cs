using FrpGUI.Models;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

namespace FrpGUI.Services
{
    public class ProcessService(FrpConfigBase frpConfig, LoggerBase logger, IEnvironmentConfig environmentConfig)
    {
        public bool IsRunning { get; set; }

        public FrpConfigBase FrpConfig { get; } = frpConfig;

        private Process frpProcess;

        public void Start()
        {
            string frpPath = environmentConfig.FrpPath;
            if (FrpConfig.Type is not ('c' or 's'))
            {
                throw new ArgumentOutOfRangeException(nameof(FrpConfig.Type));
            }

            logger.Info($"正在启动", FrpConfig);

            bool processHasStarted = false;
            try
            {
                try
                {
                    frpProcess?.Kill();
                }
                catch
                {
                }

                string configFile = Path.GetTempFileName() + ".toml";
                File.WriteAllText(configFile, FrpConfig.ToToml(), new UTF8Encoding(false));

                logger.Info("配置文件地址：" + configFile, FrpConfig);
                var frpExe = GetFrpExe(frpPath);

                if (!File.Exists(frpExe) && !File.Exists(frpExe + ".exe"))
                {
                    throw new FileNotFoundException($"没有找到frp程序，请将可执行文件放置在frp文件夹中（推荐），或在设置中配置正确的frp路径");
                }

                frpProcess = new Process();
                frpProcess.StartInfo = new ProcessStartInfo()
                {
                    FileName = frpExe,
                    Arguments = $"-c \"{configFile}\"",
                    WorkingDirectory = Path.GetDirectoryName(frpExe),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    StandardErrorEncoding = Encoding.UTF8,
                    StandardInputEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8,
                };
                frpProcess.EnableRaisingEvents = true;
                frpProcess.OutputDataReceived += P_OutputDataReceived;
                frpProcess.ErrorDataReceived += P_OutputDataReceived;
                processHasStarted = true;
                frpProcess.Start();
                frpProcess.BeginOutputReadLine();
                frpProcess.BeginErrorReadLine();
                frpProcess.Exited += FrpProcess_Exited;
                IsRunning = true;
                Started?.Invoke(this, new EventArgs());
            }
            catch (Exception ex)
            {
                logger.Error("启动失败：" + ex.Message, FrpConfig, ex);
                if (processHasStarted)
                {
                    try
                    {
                        frpProcess.Kill();
                        frpProcess.Dispose();
                    }
                    catch
                    {
                    }
                }

                throw;
            }
        }

        private string GetFrpExe(string frpPath)
        {
            string frpExe = Path.Combine(frpPath, $"frp{FrpConfig.Type}");

            // 如果是相对路径且当前目录下不存在，尝试从应用目录查找
            if (!File.Exists(frpExe) && !File.Exists(frpExe + ".exe"))
            {
                string appDirectory = AppContext.BaseDirectory;

                // 检查是否为macOS平台
                bool isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

                // 在macOS平台上，优先查找macos/frp目录
                if (isMacOS)
                {
                    string macosFrpPath = Path.Combine(appDirectory, "macos", "frp", $"frp{FrpConfig.Type}");
                    if (File.Exists(macosFrpPath) || File.Exists(macosFrpPath + ".exe"))
                    {
                        frpExe = macosFrpPath;
                    }
                }

                // 如果macOS特定路径不存在或不是macOS平台，尝试标准路径
                if (!File.Exists(frpExe) && !File.Exists(frpExe + ".exe"))
                {
                    // 1. 优先在应用目录的frp文件夹中查找（推荐存放位置）
                    string frpDirPath = Path.Combine(appDirectory, "frp", $"frp{FrpConfig.Type}");
                    if (File.Exists(frpDirPath) || File.Exists(frpDirPath + ".exe"))
                    {
                        frpExe = frpDirPath;
                    }
                    // 2. 然后尝试配置的路径
                    else if (frpPath != "./frp") // 避免重复检查
                    {
                        string altPath = Path.Combine(appDirectory, frpPath, $"frp{FrpConfig.Type}");
                        if (File.Exists(altPath) || File.Exists(altPath + ".exe"))
                        {
                            frpExe = altPath;
                        }
                    }
                    // 3. 最后尝试直接在应用程序目录中查找（向后兼容）
                    else
                    {
                        string directAppDirFrpExe = Path.Combine(appDirectory, $"frp{FrpConfig.Type}");
                        if (File.Exists(directAppDirFrpExe) || File.Exists(directAppDirFrpExe + ".exe"))
                        {
                            frpExe = directAppDirFrpExe;
                        }
                    }
                }
            }

            return frpExe;
        }

        public async Task<Process[]> GetExistedProcesses(char type)
        {
            Process[] existProcess = null;
            await Task.Run(() => { existProcess = Process.GetProcessesByName($"frp{type}"); });
            return existProcess;
        }

        public async Task KillExistedProcesses(char type)
        {
            Process[] existProcess = null;
            await Task.Run(() => { existProcess = Process.GetProcessesByName($"frp{type}"); });
            if (existProcess.Length > 0)
            {
                foreach (var p in existProcess)
                {
                    p.Kill(true);
                }
            }
        }

        private void FrpProcess_Exited(object sender, EventArgs e)
        {
            IsRunning = false;
            frpProcess.Dispose();
            frpProcess = null;
            Exited?.Invoke(sender, e);
        }

        public async Task RestartAsync()
        {
            if (frpProcess == null)
            {
                throw new Exception();
            }

            await StopAsync();
            Start();
        }

        public Task StopAsync()
        {
            if (frpProcess == null)
            {
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<int>();
            IsRunning = false;
            frpProcess.Exited -= FrpProcess_Exited;
            frpProcess.Exited += (p1, p2) =>
            {
                frpProcess.Dispose();
                int code = 0;
                try
                {
                    code = frpProcess.ExitCode;
                }
                catch
                {
                }

                frpProcess = null;
                Exited?.Invoke(this, new EventArgs());
                tcs.SetResult(code);
            };
            frpProcess.Kill(true);
            return tcs.Task;
        }

        private void P_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.Data))
            {
                return;
            }

            logger.Output(e.Data, FrpConfig);
        }

        public event EventHandler Exited;

        public event EventHandler Started;
    }
}