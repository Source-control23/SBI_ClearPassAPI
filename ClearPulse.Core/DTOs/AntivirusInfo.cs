using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace ClearPulse.Core.DTOs
{
    public class AntivirusInfo
    {
        public string vendor { get; set; }
        public string version { get; set; }
        public string dat_file_time { get; set; }
        public string engine_version { get; set; }
        public string last_scan_time { get; set; }
        public string rtp { get; set; }
    }
}
