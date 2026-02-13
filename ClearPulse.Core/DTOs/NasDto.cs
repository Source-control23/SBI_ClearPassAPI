using System;
using System.Collections.Generic;
using System.Text;

namespace ClearPulse.Core.DTOs
{
    public class NasDto
    {
        public string NasIp { get; set; }
        public string CircleCode { get; set; }
        public int RequestCount { get; set; }
        public DateTime LastSeen { get; set; }
    }

}
