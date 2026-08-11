using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ClearPulse.Core.Entities
{
 
        public class DeviceAuthEvent : BaseLogEntity
        {
            // Device Identification
            [Required, MaxLength(12)]
            public string MacAddress { get; set; } = string.Empty;
            public string? Username { get; set; }
            public string? Hostname { get; set; }
            public string? NasIp { get; set; }

            // Authentication Details
            public string? AuthType { get; set; }    // 802.1x, MAB
            public string? AuthMethod { get; set; }  // EAP-TLS, PEAP
            public int? ErrorCode { get; set; }
            public string? Reason { get; set; }
            public string? EnforcementProfile { get; set; }

        // Location
        public string? BranchCode { get; set; }

            // Quarantine Details
            public string? QuarantineReason { get; set; }
            public string? QuarantineCategory { get; set; } // AV, AD, Patch
            public string? IsolationState { get; set; }     // Isolated, Quarantined

            // AV Status
            public bool AVInstalled { get; set; }
            public bool AVUpdated { get; set; }
            public bool AVScanned { get; set; }
            public string? AVProduct { get; set; }

            // AD Status
            public bool ADAccountDisabled { get; set; }
            public bool ADUserFound { get; set; }
            public bool ADCertificateValid { get; set; }
            public string? ADDomain { get; set; }

            // Windows Patches
            public string? WindowsPatchLevel { get; set; }
            public int? CriticalPatchesMissing { get; set; }
            public DateTime? LastPatchDate { get; set; }

            // Final Result
            public string? HealthStatus { get; set; } // Healthy, Unhealthy
        }
}