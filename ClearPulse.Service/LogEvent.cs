using System;
using System.Collections.Generic;
using System.Text;

namespace ClearPulse.Service
{
    public class LogEvent1
    {
        public string SourceSystem { get; set; }
        public string EventType { get; set; }
        public string CircleCode { get; set; }
        public string Status { get; set; }
        public DateTime EventTime { get; set; }
        public string MetadataJson { get; set; }
        public string TenantId { get; set; }

        // Extra fields for DeviceAuthEvents
        public string MacAddress { get; set; }
        public string Username { get; set; }
        public string NasIp { get; set; }
        public int? ErrorCode { get; set; }
        public string Reason { get; set; }

        // Extra fields for AdIntegrations
        public string AdServer { get; set; }
        public string FailureReason { get; set; }

        // Extra fields for ClusterNodes
        public string NodeIp { get; set; }
        public string Site { get; set; }
        public string Uptime { get; set; }
        public string SyncStatus { get; set; }
    }
}
