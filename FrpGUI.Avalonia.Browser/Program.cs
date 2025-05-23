﻿using Avalonia;
using Avalonia.Browser;
using FrpGUI.Avalonia;
using FrpGUI.Avalonia.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("browser")]

internal sealed partial class Program
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>();
    }

    private static async Task Main(string[] args)
    {
        await JSHost.ImportAsync("utils.js", "../utils.js");
        try
        {
            await ProcessDefaultUIConfigAsync();
        }
        catch (Exception ex)
        {
            JsInterop.Alert("获取默认UI配置失败：" + ex.Message);
        }
        await BuildAvaloniaApp().StartBrowserAppAsync("out");
    }

    private static async Task ProcessDefaultUIConfigAsync()
    {
        UIConfig config = new UIConfig();
        string url = $"{JsInterop.GetCurrentUrl().TrimEnd('/')}/{Path.GetFileName(config.ConfigPath)}";

        HttpClient httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(2) };
        var response = await httpClient.GetAsync(url);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }
        var s = await response.Content.ReadAsStreamAsync();
        UIConfig.DefaultConfig = JsonSerializer.Deserialize(s, FrpAvaloniaSourceGenerationContext.Default.UIConfig);
    }
}