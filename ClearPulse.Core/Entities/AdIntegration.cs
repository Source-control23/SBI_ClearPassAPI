using System;
using System.Collections.Generic;
using System.Text;

namespace ClearPulse.Core.Entities
{
    public class AdIntegration : BaseLogEntity
    {
        // Maps to Requirement (i): e.g., ROOTDCMDXYZ02.AD.XYZ
        public string? AdServer { get; set; }
        public string? FailureReason { get; set; }

        // Added to track which domain is failing (e.g., PAT.AD.XYZ)
        public string? DomainName { get; set; }

        // Parsed from AD integration alerts (e.g. Error-Code=216) for faster grouping
        public int? ErrorCode { get; set; }
    }
}