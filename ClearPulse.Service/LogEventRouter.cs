using System;
using System.Collections.Generic;
using System.Text;

namespace ClearPulse.Service
{
    public static class LogEventRouter
    {
        public static string GetTargetTable(LogEvent1 evt)
        {
            if (!string.IsNullOrEmpty(evt.AdServer))
                return "AdIntegrations";
            if (!string.IsNullOrEmpty(evt.NodeIp))
                return "ClusterNodes";
            if (!string.IsNullOrEmpty(evt.MacAddress))
                return "DeviceAuthEvents";
            if (!string.IsNullOrEmpty(evt.NasIp) && string.IsNullOrEmpty(evt.Username))
                return "NasDevices";
            return "LogEvents";
        }
    }
}
