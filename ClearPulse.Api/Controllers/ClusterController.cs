using ClearPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClearPulse.Api.Controllers
{
    /// <summary>
    /// Cluster management endpoints
    /// h) Cluster-wide status monitoring
    /// </summary>
    [ApiController]
    [Route("api/cluster")]
    [Authorize]
    public class ClusterController : ControllerBase
    {
        private readonly ClearPulseDbContext _db;

        public ClusterController(ClearPulseDbContext db) => _db = db;

        /// <summary>
        /// Gets cluster-wide status with all node details
        /// Shows health, sync status, replication, and resource usage
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var tenantId = User.FindFirst("tenantId")?.Value;

            var q = _db.ClusterNodes.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                q = q.Where(c => c.TenantId == tenantId);

            // Get latest status per node
            var nodes = await q
                .GroupBy(c => c.NodeIp)
                .Select(g => g.OrderByDescending(c => c.EventTime).FirstOrDefault())
                .ToListAsync();

            var totalNodes = nodes.Count;
            var healthyNodes = nodes.Count(n => n.Status == "Healthy");
            var degradedNodes = nodes.Count(n => n.Status == "Degraded");
            var failedNodes = totalNodes - healthyNodes - degradedNodes;

            var clusterStatus = new
            {
                OverallStatus = healthyNodes == totalNodes ? "Healthy" :
                               healthyNodes >= totalNodes / 2 ? "Partial" :
                               "Critical",
                TotalNodes = totalNodes,
                HealthyNodes = healthyNodes,
                DegradedNodes = degradedNodes,
                FailedNodes = failedNodes,
                HealthPercentage = totalNodes > 0
                    ? (healthyNodes * 100.0 / totalNodes)
                    : 0,
                LastUpdated = nodes.Max(n => n.EventTime),
                Nodes = nodes.Select(n => new
                {
                    n.NodeIp,
                    n.NodeName,
                    n.NodeRole,
                    n.Status,
                    n.SyncStatus,
                    n.ReplicationStatus,
                    n.Uptime,
                    n.CpuUsage,
                    n.MemoryUsage,
                    n.Site,
                    n.EventTime,
                    IsHealthy = n.Status == "Healthy",
                    ResourceStatus = GetResourceStatus((float)n.CpuUsage, (float)n.MemoryUsage)
                }).OrderBy(n => n.NodeRole).ThenBy(n => n.NodeIp)
            };

            return Ok(clusterStatus);
        }

        /// <summary>
        /// Gets cluster health summary
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> GetHealth()
        {
            var tenantId = User.FindFirst("tenantId")?.Value;

            var q = _db.ClusterNodes.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                q = q.Where(c => c.TenantId == tenantId);

            var nodes = await q
                .GroupBy(c => c.NodeIp)
                .Select(g => g.OrderByDescending(c => c.EventTime).FirstOrDefault())
                .ToListAsync();

            var healthSummary = new
            {
                Status = nodes.All(n => n.Status == "Healthy") ? "Healthy" :
                        nodes.Any(n => n.Status == "Healthy") ? "Degraded" :
                        "Critical",
                TotalNodes = nodes.Count,
                HealthyCount = nodes.Count(n => n.Status == "Healthy"),
                Issues = nodes
                    .Where(n => n.Status != "Healthy")
                    .Select(n => new
                    {
                        n.NodeIp,
                        n.NodeName,
                        n.Status,
                        n.SyncStatus,
                        n.ReplicationStatus,
                        Issue = DetermineIssue(n)
                    })
                    .ToList()
            };

            return Ok(healthSummary);
        }

        /// <summary>
        /// Gets details for a specific cluster node
        /// </summary>
        [HttpGet("node/{nodeIp}")]
        public async Task<IActionResult> GetNodeDetails(string nodeIp)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;

            var q = _db.ClusterNodes
                .Where(c => c.NodeIp == nodeIp);

            if (!string.IsNullOrEmpty(tenantId))
                q = q.Where(c => c.TenantId == tenantId);

            // Get latest status
            var latestStatus = await q
                .OrderByDescending(c => c.EventTime)
                .FirstOrDefaultAsync();

            if (latestStatus == null)
                return NotFound($"Node with IP {nodeIp} not found");

            // Get historical status (last 24 hours)
            var history = await q
                .Where(c => c.EventTime >= DateTime.UtcNow.AddHours(-24))
                .OrderByDescending(c => c.EventTime)
                .Take(100)
                .ToListAsync();

            return Ok(new
            {
                CurrentStatus = latestStatus,
                History = history,
                Statistics = new
                {
                    AverageCpuUsage = history.Average(h => h.CpuUsage),
                    AverageMemoryUsage = history.Average(h => h.MemoryUsage),
                    HealthyPercentage = history.Count > 0
                        ? history.Count(h => h.Status == "Healthy") * 100.0 / history.Count
                        : 0,
                    LastSyncIssue = history.FirstOrDefault(h => h.SyncStatus != "Healthy")?.EventTime
                }
            });
        }

        /// <summary>
        /// Gets cluster replication status
        /// </summary>
        [HttpGet("replication")]
        public async Task<IActionResult> GetReplicationStatus()
        {
            var tenantId = User.FindFirst("tenantId")?.Value;

            var q = _db.ClusterNodes.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                q = q.Where(c => c.TenantId == tenantId);

            var nodes = await q
                .GroupBy(c => c.NodeIp)
                .Select(g => g.OrderByDescending(c => c.EventTime).FirstOrDefault())
                .ToListAsync();

            var replicationStatus = new
            {
                OverallStatus = nodes.All(n => n.ReplicationStatus == "Active") ? "Active" :
                               nodes.Any(n => n.ReplicationStatus == "Active") ? "Partial" :
                               "Failed",
                Nodes = nodes.Select(n => new
                {
                    n.NodeIp,
                    n.NodeName,
                    n.NodeRole,
                    n.ReplicationStatus,
                    n.SyncStatus,
                    n.EventTime,
                    IsReplicating = n.ReplicationStatus == "Active"
                }).ToList()
            };

            return Ok(replicationStatus);
        }

        /// <summary>
        /// [Corporate/Admin only] Gets cluster events history
        /// </summary>
        [HttpGet("events")]
        [Authorize(Policy = "Corporate")]
        public async Task<IActionResult> GetEvents(
            [FromQuery] int hours = 24,
            [FromQuery] string nodeIp = null)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var startTime = DateTime.UtcNow.AddHours(-hours);

            var q = _db.ClusterNodes
                .Where(c => c.EventTime >= startTime);

            if (!string.IsNullOrEmpty(tenantId))
                q = q.Where(c => c.TenantId == tenantId);

            if (!string.IsNullOrEmpty(nodeIp))
                q = q.Where(c => c.NodeIp == nodeIp);

            var events = await q
                .OrderByDescending(c => c.EventTime)
                .Take(500)
                .ToListAsync();

            return Ok(new
            {
                TimeRangeHours = hours,
                EventCount = events.Count,
                Events = events
            });
        }

        #region Private Helper Methods

        private string GetResourceStatus(float cpuUsage, float memoryUsage)
        {
            if (cpuUsage > 90 || memoryUsage > 90)
                return "Critical";
            if (cpuUsage > 75 || memoryUsage > 75)
                return "Warning";
            return "Normal";
        }

        private string DetermineIssue(Core.Entities.ClusterNode node)
        {
            var issues = new List<string>();

            if (node.Status != "Healthy")
                issues.Add($"Status: {node.Status}");

            if (node.SyncStatus != "Healthy")
                issues.Add($"Sync: {node.SyncStatus}");

            if (node.ReplicationStatus != "Active")
                issues.Add($"Replication: {node.ReplicationStatus}");

            if (node.CpuUsage > 90)
                issues.Add($"High CPU: {node.CpuUsage:F1}%");

            if (node.MemoryUsage > 90)
                issues.Add($"High Memory: {node.MemoryUsage:F1}%");

            return issues.Count > 0 ? string.Join(", ", issues) : "Unknown";
        }

        #endregion
    }
}