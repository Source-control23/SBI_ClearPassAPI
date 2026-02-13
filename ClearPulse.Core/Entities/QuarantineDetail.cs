using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ClearPulse.Core.Entities
{
    public class QuarantineDetail : BaseLogEntity
    {
        [Required, MaxLength(12)]
        public string MacAddress { get; set; } = string.Empty;

        public string? QuarantineType { get; set; }   // AV_NotInstalled, etc.
        public string? QuarantineReason { get; set; }
        public string? DetailedMessage { get; set; }

        public DateTime QuarantineStartTime { get; set; }
        public DateTime? QuarantineEndTime { get; set; }
        public bool IsActive { get; set; }

        public string? ResolvedBy { get; set; }      // "System" or "AdminName"
        public string? ResolutionNotes { get; set; }
    }
}
