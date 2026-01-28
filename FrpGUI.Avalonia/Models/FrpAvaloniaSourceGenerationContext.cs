using FrpGUI.Avalonia.ViewModels;
using FrpGUI.Configs;
using FrpGUI.Enums;
using FrpGUI.Models;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace FrpGUI.Avalonia.Models;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNameCaseInsensitive = true
)]
[JsonSerializable(typeof(FrpStatusInfo))]
[JsonSerializable(typeof(FrpProcess))]
[JsonSerializable(typeof(UIConfig))]
[JsonSerializable(typeof(LogEntity))]
[JsonSerializable(typeof(TokenVerification))]
[JsonSerializable(typeof(List<LogEntity>))]
[JsonSerializable(typeof(List<FrpStatusInfo>))]
[JsonSerializable(typeof(List<FrpConfigBase>))]
[JsonSerializable(typeof(List<FrpProcess>))]
[JsonSerializable(typeof(List<ProcessInfo>))]
[JsonSerializable(typeof(List<ServerConfig>))]
[JsonSerializable(typeof(List<ClientConfig>))]
public partial class FrpAvaloniaSourceGenerationContext : JsonSerializerContext
{
}

