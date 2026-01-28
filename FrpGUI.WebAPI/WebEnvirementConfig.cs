using FrpGUI.Models;

namespace FrpGUI.WebAPI;

public class WebEnvirementConfig(IConfiguration config) : IEnvironmentConfig
{
    public string FrpPath => config["FrpPath"] ?? "";
}