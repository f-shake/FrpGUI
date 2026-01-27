using FrpGUI.Models;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace FrpGUI.Configs
{
    public class AppConfig : ConfigBase
    {
        private static readonly string ConfigPathS = Path.Combine(AppContext.BaseDirectory, "config.json");
        public override string ConfigPath { get; } = ConfigPathS;
        public List<FrpConfigBase> FrpConfigs { get; set; } = new List<FrpConfigBase>();
        public string Token { get; set; }
        private JsonTypeInfo<AppConfig> JsonTypeInfo { get; } = AppConfigSourceGenerationContext.Get().AppConfig;

        public static AppConfig Get()
        {
            //MigrateConfig20250407();
            return Get(AppConfigSourceGenerationContext.Get().AppConfig);
        }

        public void Save()
        {
            Save(JsonTypeInfo);
        }

        protected override void OnLoaded()
        {
            if (FrpConfigs.Count == 0)
            {
                FrpConfigs.Add(new ServerConfig());
                FrpConfigs.Add(new ClientConfig());
            }
        }

    }
}