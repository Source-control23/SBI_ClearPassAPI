using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ClearPulse.Core.Entities
{
    public abstract class BaseLogEntity
    {
        [Key]
        public long Id { get; set; } // Primary Key for all tables

        [MaxLength(50)]
        public string? SourceSystem { get; set; } = "ClearPass";

        [MaxLength(100)]
        public string? EventType { get; set; }

        [MaxLength(20)]
        public string? CircleCode { get; set; } = "Not Known";

        public DateTime EventTime { get; set; } = DateTime.UtcNow;

        public string? TenantId { get; set; }
        public string? Status { get; set; }
        public string? MetadataJson { get; set; } // For raw log troubleshooting
    }
}
