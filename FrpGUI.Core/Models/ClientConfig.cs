﻿using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;

namespace FrpGUI.Models
{
    public partial class ClientConfig : FrpConfigBase
    {
        [ObservableProperty]
        private bool enableTls;

        [ObservableProperty]
        private bool loginFailExit = false;

        [ObservableProperty]
        private short poolCount = 50;

        [ObservableProperty]
        private List<Rule> rules = new List<Rule>();

        [ObservableProperty]
        private string serverAddress;

        [ObservableProperty]
        private ushort serverPort = 7000;

        public ClientConfig()
        {
            Name = "客户端";
        }

        public override char Type { get; } = 'c';

        public override object Clone()
        {
            var newItem = base.Clone() as ClientConfig;
            newItem.Rules = Rules.Select(p => p.Clone() as Rule).ToList();
            return newItem;
        }

        public override string ToToml()
        {
            StringBuilder str = new StringBuilder();
            str.Append("serverAddr = ").Append('"').Append(ServerAddress).Append('"').AppendLine();
            str.Append("serverPort = ").Append(ServerPort).AppendLine();
            str.Append("loginFailExit = ").Append(LoginFailExit.ToString().ToLower()).AppendLine();

            str.Append("webServer.addr = ").Append('"').Append(DashBoardAddress).Append('"').AppendLine();
            str.Append("webServer.port = ").Append(DashBoardPort).AppendLine();
            str.Append("webServer.user = ").Append('"').Append(DashBoardUsername).Append('"').AppendLine();
            str.Append("webServer.password  = ").Append('"').Append(DashBoardPassword).Append('"').AppendLine();
            if (!string.IsNullOrWhiteSpace(Token))
            {
                str.Append("auth.token = ").Append('"').Append(Token).Append('"').AppendLine();
            }

            str.Append("transport.tls.enable = ").Append(EnableTls.ToString().ToLower()).AppendLine();
            str.Append("transport.poolCount = ").Append(PoolCount).AppendLine();

            str.AppendLine();
            foreach (var rule in Rules.Where(p => p.Enable && !string.IsNullOrEmpty(p.Name)))
            {
                str.Append(rule.ToToml()).AppendLine();
            }
            str.AppendLine();
            return str.ToString();
        }

        public override void Adapt(FrpConfigBase config)
        {
            base.Adapt(config);
            if (config is not ClientConfig clientConfig)
            {
                throw new ArgumentException("必须为" + nameof(ClientConfig));
            }
            clientConfig.EnableTls = EnableTls;
            clientConfig.LoginFailExit = LoginFailExit;
            clientConfig.PoolCount = PoolCount;
            clientConfig.ServerAddress = ServerAddress;
            clientConfig.ServerPort = ServerPort;
            clientConfig.Rules = Rules.Select(rule => rule.Clone() as Rule).ToList();
        }
    }
}