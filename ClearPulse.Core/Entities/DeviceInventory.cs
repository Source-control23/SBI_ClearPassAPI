using System;
using System.ComponentModel.DataAnnotations;

namespace ClearPulse.Core.Entities
{
    /// <summary>
    /// Current (latest known) device state for fast dashboard queries.
    /// One row per MAC per tenant.
    /// </summary>
    public class DeviceInventory : BaseLogEntity
    {
        [Required, MaxLength(12)]
        public string MacAddress { get; set; } = string.Empty;
        public string? Hostname { get; set; }
        public string? IpAddress { get; set; }
        public string? DeviceType { get; set; }
        public string? OperatingSystem { get; set; }
        public string? OSVersion { get; set; }

        // Location
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }

        // Current Status
        public string? CurrentHealthStatus { get; set; }
        public DateTime? LastSeenOnline { get; set; }
        public DateTime? LastAuthSuccess { get; set; }
        public DateTime? LastAuthFailure { get; set; }

        // AV Status (Latest Known)
        public bool? AVInstalled { get; set; }
        public string? AVProduct { get; set; }
        public DateTime? AVLastUpdate { get; set; }
        public DateTime? AVLastScan { get; set; }

        // Compliance
        public bool? IsCompliant { get; set; }
        public string? ComplianceIssues { get; set; } // JSON Array of issues

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}

