using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ClearPulse.Core.Entities
{
    public class SystemUser
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(50)] // Consider 100 or 256 if you expect long corporate emails
        public string Email { get; set; } = string.Empty;

        // Role Management: Admin, CircleAdmin, Viewer
        [Required, MaxLength(20)]
        public string Role { get; set; } = "Viewer";

        // The boundary for RBAC
        [MaxLength(20)]
        public string? CircleCode { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? TenantId { get; set; }
    }

}
