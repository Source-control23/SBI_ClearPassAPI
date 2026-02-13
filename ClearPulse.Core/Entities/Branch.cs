using System.ComponentModel.DataAnnotations;

namespace ClearPulse.Core.Entities
{
    public class Branch
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(20)]
        public string BranchCode { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string BranchName { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string CircleCode { get; set; } = string.Empty; // Maps to Circle.CircleCode

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(50)]
        public string? State { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(50)]
        public string? TenantId { get; set; }
    }
}

