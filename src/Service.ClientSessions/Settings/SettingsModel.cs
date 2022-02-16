using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.ClientSessions.Settings
{
    public class SettingsModel
    {
        [YamlProperty("ClientSessions.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("ClientSessions.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("ClientSessions.ElkLogs")]
        public LogElkSettings ElkLogs { get; set; }
        
        [YamlProperty("ClientSessions.MaxSessionsCount")]
        public int MaxSessionCount { get; set; }
        
        [YamlProperty("ClientSessions.MyNoSqlWriterUrl")]
        public string MyNoSqlWriterUrl { get; set; }
        
        [YamlProperty("ClientSessions.AuthServiceBusHostPort")]
        public string AuthServiceBusHostPort { get; set; }
    }
}
