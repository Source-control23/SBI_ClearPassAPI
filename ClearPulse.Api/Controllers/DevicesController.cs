using ClearPulse.Core.DTOs;
using ClearPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ClearPulse.Api.Controllers
{
    /// <summary>
    /// Device management endpoints
    /// Provides device inventory and authentication event queries
    /// </summary>
    [ApiController]
    [Route("api/devices")]
    [Authorize]
    public class DevicesController : ControllerBase
    {
        private readonly ClearPulseDbContext _db;

        public DevicesController(ClearPulseDbContext db) => _db = db;

        /// <summary>
        /// Returns device authentication events with optional filters
        /// Supports: 802.1x/MAB filtering, Health status, Machine-only filter
        /// Circle Heads are automatically restricted to their circle
        /// </summary>
        [HttpGet]
public async Task<IActionResult> GetDevices(
     [FromQuery] string authType = null,
     [FromQuery] string healthStatus = null,
     [FromQuery] bool machinesOnly = false,
     [FromQuery] string circleCode = null,
     [FromQuery] string search = null,
     [FromQuery] string status = null,
     [FromQuery] int pageNumber = 1,
     [FromQuery] int pageSize = 10,
     [FromQuery] DateTime? fromDate = null,   // ← ADD THIS
     [FromQuery] DateTime? toDate = null)     // ← ADD THIS
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var tenantId = User.FindFirst("tenantId")?.Value;
            var userCircle = User.IsInRole("CircleHead")
                ? User.FindFirst("circleCode")?.Value
                : circleCode;

            var q = _db.DeviceAuthEvents.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                q = q.Where(d => d.TenantId == tenantId);
            if (!string.IsNullOrEmpty(userCircle))
                q = q.Where(d => d.CircleCode == userCircle);
            if (!string.IsNullOrWhiteSpace(authType))
                q = q.Where(d => d.AuthType == authType);
            if (!string.IsNullOrWhiteSpace(healthStatus))
                q = q.Where(d => d.HealthStatus == healthStatus);
            if (machinesOnly)
                q = q.Where(d => d.Username != null && d.Username.StartsWith("host/"));
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
                q = q.Where(d => d.Status == status);
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                q = q.Where(d =>
                    (d.MacAddress != null && d.MacAddress.ToLower().Contains(search)) ||
                    (d.Username != null && d.Username.ToLower().Contains(search)) ||
                    (d.NasIp != null && d.NasIp.ToLower().Contains(search)) ||
                    (d.CircleCode != null && d.CircleCode.ToLower().Contains(search))
                );
            }

            // ── Date range filter on EventTime ──────────────────────────────────
            if (fromDate.HasValue)
                q = q.Where(d => d.EventTime >= fromDate.Value);   // ← ADD THIS
            if (toDate.HasValue)
                q = q.Where(d => d.EventTime <= toDate.Value);     // ← ADD THIS

            var totalRecords = await q.CountAsync();
            var list = await q
                .OrderByDescending(d => d.EventTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DeviceDto
                {
                    MacAddress = d.MacAddress ?? string.Empty,
                    Username = d.Username ?? string.Empty,
                    NasIp = d.NasIp ?? string.Empty,
                    CircleCode = d.CircleCode ?? string.Empty,
                    AuthType = d.AuthType ?? string.Empty,
                    Status = d.Status ?? string.Empty,
                    Reason = d.Reason,
                    EventTime = d.EventTime
                })
                .ToListAsync();

            return Ok(new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = list
            });
        }

        /// <summary>
        /// Gets device inventory (current state, one record per device)
        /// </summary>
        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventory(
            [FromQuery] string circleCode = null,
            [FromQuery] string healthStatus = null,
            [FromQuery] bool? compliant = null,
            [FromQuery] int take = 500)
        {
            if (take <= 0) take = 100;
            if (take > 2000) take = 2000;

            var tenantId = User.FindFirst("tenantId")?.Value;
            var userCircle = User.IsInRole("CircleHead")
                ? User.FindFirst("circleCode")?.Value
                : circleCode;

            var q = _db.DeviceInventory.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                q = q.Where(d => d.TenantId == tenantId);

            if (!string.IsNullOrEmpty(userCircle))
                q = q.Where(d => d.CircleCode == userCircle);

            if (!string.IsNullOrEmpty(healthStatus))
                q = q.Where(d => d.CurrentHealthStatus == healthStatus);

            if (compliant.HasValue)
                q = q.Where(d => d.IsCompliant == compliant.Value);

            var list = await q
                .OrderByDescending(d => d.LastSeenOnline)
                .Take(take)
                .ToListAsync();

            return Ok(list);
        }

        /// <summary>
        /// Gets device details by MAC address
        /// </summary>
        [HttpGet("{macAddress}")]
        public async Task<IActionResult> GetDeviceByMac(string macAddress)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var userCircle = User.IsInRole("CircleHead")
                ? User.FindFirst("circleCode")?.Value
                : null;

            // Get from inventory
            var deviceQuery = _db.DeviceInventory.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                deviceQuery = deviceQuery.Where(d => d.TenantId == tenantId);

            if (!string.IsNullOrEmpty(userCircle))
                deviceQuery = deviceQuery.Where(d => d.CircleCode == userCircle);

            var device = await deviceQuery
                .Where(d => d.MacAddress == macAddress)
                .FirstOrDefaultAsync();

            if (device == null)
                return NotFound($"Device with MAC address {macAddress} not found");

            // Get recent auth events for this device
            var recentEvents = await _db.DeviceAuthEvents
                .Where(e => e.MacAddress == macAddress)
                .OrderByDescending(e => e.EventTime)
                .Take(10)
                .ToListAsync();

            return Ok(new
            {
                Device = device,
                RecentEvents = recentEvents
            });
        }

        /// <summary>
        /// Gets device count summary by various categories
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var userCircle = User.IsInRole("CircleHead")
                ? User.FindFirst("circleCode")?.Value
                : null;

            var q = _db.DeviceInventory.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                q = q.Where(d => d.TenantId == tenantId);

            if (!string.IsNullOrEmpty(userCircle))
                q = q.Where(d => d.CircleCode == userCircle);

            var devices = await q.ToListAsync();

            var summary = new
            {
                TotalDevices = devices.Count,
                HealthyDevices = devices.Count(d => d.CurrentHealthStatus == "Healthy"),
                UnhealthyDevices = devices.Count(d => d.CurrentHealthStatus == "Unhealthy"),
                CompliantDevices = devices.Count(d => d.IsCompliant == true),
                NonCompliantDevices = devices.Count(d => d.IsCompliant == false),
                DevicesWithAV = devices.Count(d => d.AVInstalled == true),
                DevicesWithoutAV = devices.Count(d => d.AVInstalled == false)
            };

            return Ok(summary);
        }
    }
}