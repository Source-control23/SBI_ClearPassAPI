using System;
using System.Collections.Generic;
using System.Text;

namespace ClearPulse.Core.DTOs
{
    public class MetricsDto
    {
        public int Auth8021xCount { get; set; }
        public int MabCount { get; set; }
        public int HealthyCount { get; set; }
        public int UnhealthyCount { get; set; }
        public int NasTotalCount { get; set; }
        public int NasActiveCount { get; set; }
        public DateTime SnapshotTime { get; set; }
    }

}
