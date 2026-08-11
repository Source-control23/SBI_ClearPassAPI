using System;
using System.Collections.Generic;
using System.Text;

namespace ClearPulse.Core.DTOs
{
    public class DeviceDto
    {
        public string MacAddress { get; set; }
        public string Username { get; set; }
        public string NasIp { get; set; }
        public string CircleCode { get; set; }
        public string AuthType { get; set; }
        public string Status { get; set; }
        public string HealthStatus { get; set; }
        public string EnforcementProfile { get; set; }
        public string? Reason { get; set; }
        public DateTime? EventTime { get; set; }
    }

}
