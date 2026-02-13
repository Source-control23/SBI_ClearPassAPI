using System.ComponentModel.DataAnnotations;

namespace ClearPulse.Core.Entities
{
    public class Circle
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(20)]
        public string CircleCode { get; set; } = string.Empty; // Primary lookup code

        [Required, MaxLength(100)]
        public string CircleName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [MaxLength(50)]
        public string? Region { get; set; }

        [MaxLength(50)]
        public string? State { get; set; }

        [MaxLength(50)] // Recommended to add a limit
        public string? TenantId { get; set; }
    }
}

