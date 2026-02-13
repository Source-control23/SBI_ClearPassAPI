using System;
using System.Collections.Generic;
using System.Text;

namespace ClearPulse.Core.DTOs
{
    public class ClusterStatusDto
    {
        public string NodeIp { get; set; }
        public string Site { get; set; }
        public string Uptime { get; set; }
        public string SyncStatus { get; set; }
        public DateTime LastUpdated { get; set; }
    }

}
