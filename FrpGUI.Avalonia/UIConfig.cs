using FrpGUI.Avalonia.Models;
using FrpGUI.Configs;
using FzLib;
using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization.Metadata;
using FrpGUI.Models;

namespace FrpGUI.Avalonia;

public class UIConfig : AppConfigBase, IAppConfig, INotifyPropertyChanged
{
    private RunningMode runningMode;

    private bool showTrayIcon;

    public UIConfig() : base()
    {
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public static UIConfig DefaultConfig { get; set; }

    [JsonIgnore]
    public override string ConfigPath => Path.Combine(AppContext.BaseDirectory, "uiconfig.json");

    public RunningMode RunningMode
    {
        get => runningMode;
        set
        {
            runningMode = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RunningMode)));
        }
    }

    public string ServerAddress { get; set; } = "http://localhost:5113";

    public string ServerToken { get; set; } = "";

    public string FrpPath { get; set; } = "./frp";

    public bool ShowTrayIcon
    {
        get => showTrayIcon;
        set
        {
            showTrayIcon = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowTrayIcon)));
        }
    }

    private static JsonTypeInfo<UIConfig> JsonTypeInfo { get; } = FrpAvaloniaSourceGenerationContext.Get().UIConfig;

    public static UIConfig Get()
    {
        return Get(JsonTypeInfo);
    }

    public void Save()
    {
        if (OperatingSystem.IsBrowser())
        {
            var json = JsonSerializer.Serialize(this, JsonTypeInfo);
            JsInterop.SetLocalStorage("config", json);
        }
        else
        {
            Save(JsonTypeInfo);
        }
    }

    protected override T GetImpl<T>(JsonTypeInfo<T> jsonTypeInfo)
    {
        if (OperatingSystem.IsBrowser())
        {
            try
            {
                var json = JsInterop.GetLocalStorage("config");
                if (string.IsNullOrEmpty(json))
                {
                    if (DefaultConfig != null)
                    {
                        return DefaultConfig as T; //优先级2：默认配置。由于HttpClient不支持同步，所以DefaultConfig在Browser项目中进行了赋值
                    }

                    return new UIConfig() as T; //优先级3：新配置
                }

                //优先级1：LocalStorage配置
                return JsonSerializer.Deserialize<UIConfig>(JsInterop.GetLocalStorage("config"), JsonTypeInfo) as T;
            }
            catch (Exception ex)
            {
                JsInterop.Alert("读取配置文件错误：" + ex.ToString());
                throw;
            }
        }
        else
        {
            return base.GetImpl<T>(jsonTypeInfo);
        }
    }
}