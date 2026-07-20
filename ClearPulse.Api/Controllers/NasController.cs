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
        public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly = null,[FromQuery] string circleCode = null,[FromQuery] string? search = null,[FromQuery] int pageNumber = 1,[FromQuery] int pageSize = 10, [FromQuery] string? timeFilter = "today")
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var userCircle = User.IsInRole("CircleHead")
                ? User.FindFirst("circleCode")?.Value
                : circleCode;

            var q = _db.NasDevices.AsQueryable();

            if (!string.IsNullOrWhiteSpace(timeFilter))
            {
                var today = DateTime.Today;

                if (timeFilter.Equals("today", StringComparison.OrdinalIgnoreCase))
                {
                    q = q.Where(x => x.LastSeen >= today);
                }
                else if (timeFilter.Equals("week", StringComparison.OrdinalIgnoreCase))
                {
                    var weekStart = today.AddDays(-6);

                    q = q.Where(x => x.LastSeen >= weekStart);
                }
            }

            // Tenant filter
            if (!string.IsNullOrEmpty(tenantId))
                q = q.Where(n => n.TenantId == tenantId);

            // Circle filter
            if (!string.IsNullOrEmpty(userCircle))
                q = q.Where(n => n.CircleCode == userCircle);

            // Search filter
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                q = q.Where(n =>
     (n.NasIp ?? "").ToLower().Contains(search) ||
     (n.CircleCode ?? "").ToLower().Contains(search) ||
     (n.SourceSystem ?? "").ToLower().Contains(search) ||
     (n.EventType ?? "").ToLower().Contains(search)
                );
            }

            // ✅ Summary counts BEFORE applying activeOnly filter
            // so cards always show real totals for the tenant/circle/search
            var totalNASDevices = await q.CountAsync();
            var activeNASDevices = await q.CountAsync(n => n.IsActive && n.RequestCount > 0);
            var inactiveNASDevices = await q.CountAsync(n => !n.IsActive || n.RequestCount == 0);

            // Active filter applied AFTER summary counts — only affects table rows
            if (activeOnly.HasValue)
            {
                if (activeOnly.Value)
                    q = q.Where(n => n.IsActive && n.RequestCount > 0);
                else
                    q = q.Where(n => !n.IsActive || n.RequestCount == 0);
            }

            // Total records for pagination (respects activeOnly)
            var totalRecords = await q.CountAsync();

            // Sorting + Pagination
            var devices = await q
                .OrderByDescending(n => n.LastSeen)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                TotalNASDevices = totalNASDevices,
                ActiveNASDevices = activeNASDevices,
                InactiveNASDevices = inactiveNASDevices,
                Devices = devices
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