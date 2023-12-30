﻿using FrpGUI.Config;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FrpGUI.Util
{
    public class ProcessHelper
    {
        public bool IsRunning { get; set; }

        private Process frpProcess;
        private string type;
        private IToFrpConfig obj;

        public void StartServer(IToFrpConfig obj)
        {
            Start("s", obj);
        }

        public void StartClient(IToFrpConfig obj)
        {
            Start("c", obj);
        }

        public void Start(string type, IToFrpConfig obj)
        {
            string typeText = type switch
            {
                "c" => "客户端",
                "s" => "服务端",
                _ => throw new Exception("不支持c和s以外的类型")
            };
            Logger.Info($"正在启动{typeText}");

            if (frpProcess != null)
            {
                frpProcess.Kill();
            }
            this.type = type;
            this.obj = obj;
            string tempDir = Path.Combine(FzLib.Program.App.ProgramDirectoryPath, "temp");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            string configFile = null;
            switch (AppConfig.Instance.FrpConfigType)
            {
                case "INI":
                    configFile = Path.Combine(tempDir, Guid.NewGuid().ToString() + ".ini");
                    File.WriteAllText(configFile, obj.ToIni());
                    break;
                case "TOML":
                    configFile = Path.Combine(tempDir, Guid.NewGuid().ToString() + ".toml");
                    File.WriteAllText(configFile, obj.ToToml(), new UTF8Encoding(false));
                    break;
                default:
                    throw new Exception("未知FRP配置文件类型");
            }
            frpProcess = new Process();
            frpProcess.StartInfo = new ProcessStartInfo()
            {
                FileName = $"./frp/frp{type}.exe",
                Arguments = $"-c \"{configFile}\"",
                WorkingDirectory = "./frp",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
                StandardInputEncoding = System.Text.Encoding.UTF8,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
            };
            frpProcess.EnableRaisingEvents = true;
            frpProcess.OutputDataReceived += P_OutputDataReceived;
            frpProcess.ErrorDataReceived += P_OutputDataReceived;
            frpProcess.Start();
            frpProcess.BeginOutputReadLine();
            frpProcess.BeginErrorReadLine();
            frpProcess.Exited += FrpProcess_Exited;
            IsRunning = true;
            Started?.Invoke(this, new EventArgs());
        }

        public async Task<Process[]> GetExistedProcesses(string type)
        {
            Process[] existProcess = null;
            await Task.Run(() =>
            {
                existProcess = Process.GetProcessesByName($"frp{type}");
            });
            return existProcess;
        }

        public async Task KillExistedProcesses(string type)
        {
            Process[] existProcess = null;
            await Task.Run(() =>
            {
                existProcess = Process.GetProcessesByName($"frp{type}");
            });
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
            Start(type, obj);
        }

        public Task StopAsync()
        {
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
            if (e.Data == null)
            {
                return;
            }
            Debug.WriteLine(e.Data);
            Logger.Ouput(e.Data);
        }

        public event EventHandler Exited;

        public event EventHandler Started;
    }
}