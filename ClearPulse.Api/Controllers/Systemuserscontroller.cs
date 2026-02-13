using ClearPulse.Core.Entities;
using ClearPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClearPulse.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class SystemUsersController : ControllerBase
    {
        private readonly ClearPulseDbContext _db;

        public SystemUsersController(ClearPulseDbContext db) => _db = db;

        /// <summary>
        /// Get all system users
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _db.SystemUsers
                .OrderBy(u => u.Username)
                .ToListAsync();

            // Don't return password hashes
            var result = users.Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.Role,
                u.CircleCode,
                u.TenantId,
                u.IsActive,
                u.CreatedDate,
                u.LastLogin
            });

            return Ok(result);
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _db.SystemUsers.FindAsync(id);

            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.CircleCode,
                user.TenantId,
                user.IsActive,
                user.CreatedDate,
                user.LastLogin
            });
        }

        /// <summary>
        /// Create new user
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SystemUserDto dto)
        {
            if (await _db.SystemUsers.AnyAsync(u => u.Username == dto.Username))
                return BadRequest("Username already exists");

            var user = new SystemUser
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                CircleCode = dto.CircleCode,
                TenantId = dto.TenantId,
                IsActive = dto.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            _db.SystemUsers.Add(user);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.CircleCode,
                user.TenantId,
                user.IsActive
            });
        }

        /// <summary>
        /// Update user
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SystemUserDto dto)
        {
            var user = await _db.SystemUsers.FindAsync(id);

            if (user == null)
                return NotFound();

            // Check username uniqueness if changed
            if (user.Username != dto.Username &&
                await _db.SystemUsers.AnyAsync(u => u.Username == dto.Username && u.Id != id))
                return BadRequest("Username already exists");

            user.Username = dto.Username;
            user.Email = dto.Email;
            user.Role = dto.Role;
            user.CircleCode = dto.CircleCode;
            user.TenantId = dto.TenantId;
            user.IsActive = dto.IsActive;

            // Only update password if provided
            if (!string.IsNullOrEmpty(dto.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.CircleCode,
                user.TenantId,
                user.IsActive
            });
        }

        /// <summary>
        /// Delete user
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _db.SystemUsers.FindAsync(id);

            if (user == null)
                return NotFound();

            _db.SystemUsers.Remove(user);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Toggle user active status
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _db.SystemUsers.FindAsync(id);

            if (user == null)
                return NotFound();

            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();

            return Ok(new { user.Id, user.IsActive });
        }
    }

    public class SystemUserDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string CircleCode { get; set; }
        public string TenantId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}