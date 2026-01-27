using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace FrpGUI.Configs
{
    public abstract class ConfigBase
    {
        public abstract string ConfigPath { get; }

        public static T Get<T>(JsonTypeInfo<T> jsonTypeInfo) where T : ConfigBase, new()
        {
            T config = new T();

            if (OperatingSystem.IsBrowser()
                || File.Exists(config.ConfigPath))
            {
                try
                {
                    config = config.GetImpl<T>(jsonTypeInfo);
                }
                catch (Exception ex)
                {
                    config = new T();
                }
            }
            config.OnLoaded();
            return config;
        }

        protected virtual T GetImpl<T>(JsonTypeInfo<T> jsonTypeInfo) where T : ConfigBase
        {
            return JsonSerializer.Deserialize(File.ReadAllText(ConfigPath), jsonTypeInfo);
        }

        public  void Save<T>(JsonTypeInfo<T> jsonTypeInfo)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(this, jsonTypeInfo);
            File.WriteAllBytes(ConfigPath, bytes);
        }

        protected virtual void OnLoaded()
        {
        }
    }
}