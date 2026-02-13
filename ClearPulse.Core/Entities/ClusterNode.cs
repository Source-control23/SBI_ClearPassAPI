using System;
using System.Collections.Generic;
using System.Text;

namespace ClearPulse.Core.Entities
{
    public class ClusterNode : BaseLogEntity
    {
        // Identification
        public string? NodeName { get; set; }
        public string? NodeIp { get; set; }
        public string? NodeRole { get; set; } // e.g., "Publisher", "Subscriber", "Standby Publisher"
        public string? Site { get; set; }     // Specific Data Center or Location

        // Health Metrics
        public string? Uptime { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }

        // Sync & Replication
        public string? SyncStatus { get; set; }        // Overall Sync (Enabled/Disabled)
        public string? ReplicationStatus { get; set; } // DB Replication State (In-Sync/Out-of-Sync)
    }
}