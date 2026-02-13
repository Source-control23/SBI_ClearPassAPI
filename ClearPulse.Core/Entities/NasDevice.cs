using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ClearPulse.Core.Entities
{
    public class NasDevice : BaseLogEntity
    {
        [Required, MaxLength(50)]
        public string? NasIp { get; set; } = string.Empty;

        public int RequestCount { get; set; }
        public DateTime LastSeen { get; set; }

        // Location Tracking
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }

        // Hardware Details
        public string? DeviceModel { get; set; }
        public string? FirmwareVersion { get; set; }

        public bool IsActive { get; set; } = true;
    }
}