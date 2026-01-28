using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace FrpGUI.Models
{
    [JsonDerivedType(typeof(ClientConfig))]
    [JsonDerivedType(typeof(ServerConfig))]
    [JsonConverter(typeof(FrpConfigJsonConverter))]
    public abstract partial class FrpConfigBase : ObservableObject, IToFrpConfig, ICloneable
    {
        [ObservableProperty]
        private bool autoStart;
        
        [ObservableProperty]
        private bool enableDashBoard;

        [ObservableProperty]
        private string dashBoardAddress = "localhost";

        [ObservableProperty]
        private string dashBoardPassword = "admin";

        [ObservableProperty]
        private ushort dashBoardPort = 7500;

        [ObservableProperty]
        private string dashBoardUsername = "admin";

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string token = "";

        public FrpConfigBase()
        {
        }

        public string ID { get; set; } = Guid.NewGuid().ToString();

        public abstract char Type { get; }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        public abstract string ToToml();

        public virtual void Adapt(FrpConfigBase config)
        {
            config.AutoStart = AutoStart;
            config.EnableDashBoard = EnableDashBoard;
            config.DashBoardPassword = DashBoardPassword;
            config.DashBoardPort = DashBoardPort;
            config.DashBoardUsername = DashBoardUsername;
            config.DashBoardAddress = DashBoardAddress;
            config.Name = Name;
            config.Token = Token;
            config.ID = ID;
        }
    }
}