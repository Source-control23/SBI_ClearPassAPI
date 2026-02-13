using ClearPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClearPulse.Api.Controllers
{
    /// <summary>
    /// NAS Device management endpoints
    /// e) Total number of NAS devices and which are sending RADIUS requests
    /// </summary>
    [ApiController]
    [Route("api/nas")]
    [Authorize]
    public class NasController : ControllerBase
    {
        private readonly ClearPulseDbContext _db;

        public NasController(ClearPulseDbContext db) => _db = db;

        /// <summary>
        /// Gets all NAS devices with request statistics
        /// Shows total NAS and which ones are actively sending RADIUS requests
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] bool? activeOnly = null,
            [FromQuery] string circleCode = null)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var userCircle = User.IsInRole("CircleHead")
                ? User.FindFirst("circleCode")?.Value
                : circleCode;

            var q = _db.NasDevices.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                q = q.Where(n => n.TenantId == tenantId);

            if (!string.IsNullOrEmpty(userCircle))
                q = q.Where(n => n.CircleCode == userCircle);

            if (activeOnly.HasValue && activeOnly.Value)
                q = q.Where(n => n.IsActive && n.RequestCount > 0);

            var list = await q
                .OrderByDescending(n => n.LastSeen)
                .ToListAsync();

            return Ok(new
            {
                TotalNASDevices = list.Count,
                ActiveNASDevices = list.Count(n => n.IsActive && n.RequestCount > 0),
                InactiveNASDevices = list.Count(n => !n.IsActive || n.RequestCount == 0),
                Devices = list
            });
        }

        /// <summary>
        /// Gets NAS device statistics summary
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var userCircle = User.IsInRole("CircleHead")
                ? User.FindFirst("circleCode")?.Value
                : null;

            var q = _db.NasDevices.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                q = q.Where(n => n.TenantId == tenantId);

            if (!string.IsNullOrEmpty(userCircle))
                q = q.Where(n => n.CircleCode == userCircle);

            var devices = await q.ToListAsync();

            var summary = new
            {
                TotalNAS = devices.Count,
                ActiveNAS = devices.Count(n => n.IsActive && n.RequestCount > 0),
                InactiveNAS = devices.Count(n => !n.IsActive || n.RequestCount == 0),
                TotalRequests = devices.Sum(n => n.RequestCount),
                AverageRequestsPerNAS = devices.Count > 0
                    ? devices.Average(n => n.RequestCount)
                    : 0,
                ByCircle = devices
                    .GroupBy(n => n.CircleCode ?? "UNKNOWN")
                    .Select(g => new
                    {
                        CircleCode = g.Key,
                        Count = g.Count(),
                        Active = g.Count(n => n.IsActive && n.RequestCount > 0)
                    })
                    .OrderBy(x => x.CircleCode)
                    .ToList()
            };

            return Ok(summary);
        }

        /// <summary>
        /// Gets details for a specific NAS device by IP
        /// </summary>
        [HttpGet("{nasIp}")]
        public async Task<IActionResult> GetByIp(string nasIp)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;

            var device = await _db.NasDevices
                .Where(n => n.NasIp == nasIp)
                .Where(n => string.IsNullOrEmpty(tenantId) || n.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (device == null)
                return NotFound($"NAS device with IP {nasIp} not found");

            // Get recent auth events from this NAS
            var recentEvents = await _db.DeviceAuthEvents
                .Where(e => e.NasIp == nasIp)
                .OrderByDescending(e => e.EventTime)
                .Take(20)
                .Select(e => new
                {
                    e.MacAddress,
                    e.Username,
                    e.AuthType,
                    e.Status,
                    e.EventTime
                })
                .ToListAsync();

            return Ok(new
            {
                Device = device,
                RecentAuthEvents = recentEvents
            });
        }

        /// <summary>
        /// [Corporate/Admin only] Gets NAS devices with no recent activity
        /// </summary>
        [HttpGet("inactive")]
        [Authorize(Policy = "Corporate")]
        public async Task<IActionResult> GetInactive(
            [FromQuery] int hoursThreshold = 24)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var threshold = DateTime.UtcNow.AddHours(-hoursThreshold);

            var q = _db.NasDevices.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                q = q.Where(n => n.TenantId == tenantId);

            var inactiveDevices = await q
                .Where(n => n.LastSeen < threshold || n.RequestCount == 0)
                .OrderBy(n => n.LastSeen)
                .ToListAsync();

            return Ok(new
            {
                HoursThreshold = hoursThreshold,
                InactiveCount = inactiveDevices.Count,
                Devices = inactiveDevices
            });
        }
    }
}