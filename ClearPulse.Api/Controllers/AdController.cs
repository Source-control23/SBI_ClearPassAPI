using ClearPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClearPulse.Api.Controllers
{
    /// <summary>
    /// Active Directory Integration endpoints
    /// i) AD integration status with details
    /// </summary>
    [ApiController]
    [Route("api/ad")]
    [Authorize]
    public class AdController : ControllerBase
    {
        private readonly ClearPulseDbContext _db;

        public AdController(ClearPulseDbContext db) => _db = db;

        /// <summary>
        /// Gets AD integration status with detailed metrics
        /// Shows success rate, failures, and server health
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus(
            [FromQuery] int hours = 24)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var userCircle = User.IsInRole("CircleHead")
                ? User.FindFirst("circleCode")?.Value
                : null;

            var startTime = DateTime.UtcNow.AddHours(-hours);

            var q = _db.AdIntegrations
                .Where(a => a.EventTime >= startTime);

            if (!string.IsNullOrEmpty(tenantId))
                q = q.Where(a => a.TenantId == tenantId);

            if (!string.IsNullOrEmpty(userCircle))
                q = q.Where(a => a.CircleCode == userCircle);

            var events = await q.ToListAsync();

            var totalEvents = events.Count;
            var successEvents = events.Count(e => e.Status == "Success");
            var failedEvents = events.Count(e => e.Status == "Failed");

            // Group by server
            var serverStats = events
                .GroupBy(e => e.AdServer ?? "Unknown")
                .Select(g => new
                {
                    Server = g.Key,
                    TotalEvents = g.Count(),
                    SuccessCount = g.Count(e => e.Status == "Success"),
                    FailureCount = g.Count(e => e.Status == "Failed"),
                    SuccessRate = g.Count() > 0
                        ? (g.Count(e => e.Status == "Success") * 100.0 / g.Count())
                        : 0,
                    LastSeen = g.Max(e => e.EventTime),
                    CommonErrors = g.Where(e => e.Status == "Failed")
                                    .GroupBy(e => e.ErrorCode)
                                    .Select(eg => new
                                    {
                                        ErrorCode = eg.Key,
                                        Count = eg.Count()
                                    })
                                    .OrderByDescending(eg => eg.Count)
                                    .Take(3)
                                    .ToList()
                })
                .OrderByDescending(s => s.FailureCount)
                .ToList();

            // Recent failures
            var recentFailures = events
                .Where(e => e.Status == "Failed")
                .OrderByDescending(e => e.EventTime)
                .Take(20)
                .Select(e => new
                {
                    e.AdServer,
                    e.DomainName,
                    e.ErrorCode,
                    e.FailureReason,
                    e.CircleCode,
                    e.EventTime
                })
                .ToList();

            var adStatus = new
            {
                TimeRangeHours = hours,
                OverallStatus = failedEvents == 0 ? "Healthy" :
                               successEvents > failedEvents * 2 ? "Warning" :
                               "Critical",
                TotalEvents = totalEvents,
                SuccessfulEvents = successEvents,
                FailedEvents = failedEvents,
                SuccessRate = totalEvents > 0
                    ? (successEvents * 100.0 / totalEvents)
                    : 100,
                LastUpdated = events.Count > 0
                    ? events.Max(e => e.EventTime)
                    : (DateTime?)null,
                ServerStatistics = serverStats,
                RecentFailures = recentFailures,
                ErrorCodeBreakdown = events
                    .Where(e => e.ErrorCode.HasValue)
                    .GroupBy(e => e.ErrorCode.Value)
                    .Select(g => new
                    {
                        ErrorCode = g.Key,
                        Count = g.Count(),
                        Description = GetErrorDescription(g.Key)
                    })
                    .OrderByDescending(e => e.Count)
                    .ToList()
            };

            return Ok(adStatus);
        }

        /// <summary>
        /// Gets AD server health details
        /// </summary>
        [HttpGet("servers")]
        public async Task<IActionResult> GetServerHealth()
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var last24Hours = DateTime.UtcNow.AddHours(-24);

            var q = _db.AdIntegrations
                .Where(a => a.EventTime >= last24Hours);

            if (!string.IsNullOrEmpty(tenantId))
                q = q.Where(a => a.TenantId == tenantId);

            var events = await q.ToListAsync();

            var servers = events
                .GroupBy(e => new { e.AdServer, e.DomainName })
                .Select(g => new
                {
                    Server = g.Key.AdServer ?? "Unknown",
                    Domain = g.Key.DomainName ?? "Unknown",
                    Status = g.Any(e => e.Status == "Failed") ? "Degraded" : "Healthy",
                    TotalRequests = g.Count(),
                    SuccessfulRequests = g.Count(e => e.Status == "Success"),
                    FailedRequests = g.Count(e => e.Status == "Failed"),
                    SuccessRate = g.Count() > 0
                        ? (g.Count(e => e.Status == "Success") * 100.0 / g.Count())
                        : 0,
                    LastSuccess = g.Where(e => e.Status == "Success")
                                   .Max(e => (DateTime?)e.EventTime),
                    LastFailure = g.Where(e => e.Status == "Failed")
                                   .Max(e => (DateTime?)e.EventTime),
                    LastSeen = g.Max(e => e.EventTime)
                })
                .OrderBy(s => s.Domain)
                .ThenBy(s => s.Server)
                .ToList();

            return Ok(new
            {
                TotalServers = servers.Count,
                HealthyServers = servers.Count(s => s.Status == "Healthy"),
                DegradedServers = servers.Count(s => s.Status == "Degraded"),
                Servers = servers
            });
        }

        /// <summary>
        /// Gets AD integration events history
        /// </summary>
        [HttpGet("events")]
        public async Task<IActionResult> GetEvents(
            [FromQuery] int hours = 24,
            [FromQuery] string status = null,
            [FromQuery] int? errorCode = null,
            [FromQuery] int take = 200)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var userCircle = User.IsInRole("CircleHead")
                ? User.FindFirst("circleCode")?.Value
                : null;

            var startTime = DateTime.UtcNow.AddHours(-hours);

            var q = _db.AdIntegrations
                .Where(a => a.EventTime >= startTime);

            if (!string.IsNullOrEmpty(tenantId))
                q = q.Where(a => a.TenantId == tenantId);

            if (!string.IsNullOrEmpty(userCircle))
                q = q.Where(a => a.CircleCode == userCircle);

            if (!string.IsNullOrEmpty(status))
                q = q.Where(a => a.Status == status);

            if (errorCode.HasValue)
                q = q.Where(a => a.ErrorCode == errorCode.Value);

            var events = await q
                .OrderByDescending(a => a.EventTime)
                .Take(take)
                .ToListAsync();

            return Ok(new
            {
                TimeRangeHours = hours,
                EventCount = events.Count,
                Events = events
            });
        }

        /// <summary>
        /// Gets AD error code statistics
        /// </summary>
        [HttpGet("errors")]
        public async Task<IActionResult> GetErrorStatistics(
            [FromQuery] int hours = 24)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var startTime = DateTime.UtcNow.AddHours(-hours);

            var q = _db.AdIntegrations
                .Where(a => a.EventTime >= startTime && a.ErrorCode.HasValue);

            if (!string.IsNullOrEmpty(tenantId))
                q = q.Where(a => a.TenantId == tenantId);

            var errors = await q.ToListAsync();

            var errorStats = errors
                .GroupBy(e => e.ErrorCode.Value)
                .Select(g => new
                {
                    ErrorCode = g.Key,
                    Description = GetErrorDescription(g.Key),
                    Count = g.Count(),
                    AffectedServers = g.Select(e => e.AdServer).Distinct().Count(),
                    AffectedCircles = g.Select(e => e.CircleCode).Distinct().Count(),
                    FirstOccurrence = g.Min(e => e.EventTime),
                    LastOccurrence = g.Max(e => e.EventTime),
                    ExampleMessage = g.First().FailureReason
                })
                .OrderByDescending(e => e.Count)
                .ToList();

            return Ok(new
            {
                TimeRangeHours = hours,
                TotalErrors = errors.Count,
                UniqueErrorCodes = errorStats.Count,
                ErrorBreakdown = errorStats
            });
        }

        /// <summary>
        /// [Corporate/Admin only] Gets AD integration summary by circle
        /// </summary>
        [HttpGet("circles")]
        [Authorize(Policy = "Corporate")]
        public async Task<IActionResult> GetCircleSummary(
            [FromQuery] int hours = 24)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var startTime = DateTime.UtcNow.AddHours(-hours);

            var q = _db.AdIntegrations
                .Where(a => a.EventTime >= startTime);

            if (!string.IsNullOrEmpty(tenantId))
                q = q.Where(a => a.TenantId == tenantId);

            var events = await q.ToListAsync();

            var circleSummary = events
                .GroupBy(e => e.CircleCode ?? "UNKNOWN")
                .Select(g => new
                {
                    CircleCode = g.Key,
                    TotalEvents = g.Count(),
                    SuccessCount = g.Count(e => e.Status == "Success"),
                    FailureCount = g.Count(e => e.Status == "Failed"),
                    SuccessRate = g.Count() > 0
                        ? (g.Count(e => e.Status == "Success") * 100.0 / g.Count())
                        : 0,
                    MostCommonError = g.Where(e => e.ErrorCode.HasValue)
                                       .GroupBy(e => e.ErrorCode.Value)
                                       .OrderByDescending(eg => eg.Count())
                                       .Select(eg => new { ErrorCode = eg.Key, Count = eg.Count() })
                                       .FirstOrDefault()
                })
                .OrderBy(c => c.CircleCode)
                .ToList();

            return Ok(circleSummary);
        }

        #region Private Helper Methods

        private string GetErrorDescription(int errorCode)
        {
            return errorCode switch
            {
                0 => "Success",
                52 => "LDAP Bind Timeout",
                216 => "User Not Found in Global Catalog",
                226 => "Account Disabled or Expired",
                525 => "User Not Found",
                530 => "Not Permitted to Logon at This Time",
                531 => "Not Permitted to Logon at This Workstation",
                532 => "Password Expired",
                533 => "Account Disabled",
                534 => "User Has Not Been Granted Requested Logon Type",
                701 => "Account Expired",
                773 => "User Must Reset Password",
                775 => "Account Locked Out",
                _ => $"Error Code {errorCode}"
            };
        }

        #endregion
    }
}