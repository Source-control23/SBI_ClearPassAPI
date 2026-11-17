using ClearPulse.Core.DTOs;
using ClearPulse.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace ClearPulse.Api.Controllers
{
    /// <summary>
    /// Dashboard API endpoints meeting all SBI requirements
    /// Supports Corporate (all circles) and Circle Head (specific circle) views
    /// </summary>
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Gets unified dashboard with all metrics
        /// Corporate: sees all circles | Circle Head: sees only their circle
        /// Includes: a) 802.1x, b) MAB, c) Healthy, d) Unhealthy, e) NAS, f) Circle breakdown, h) Cluster, i) AD
        /// </summary>
        [HttpGet("unified")]
        public async Task<ActionResult<DashboardMetricsDto>> GetUnifiedDashboard([FromQuery] string timeFilter = "custom", [FromQuery] string? circleCode = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            // Circle Head should only see their own circle
            if (User.IsInRole("CircleHead"))
            {
                circleCode = User.FindFirst("circleCode")?.Value;
            }

            var metrics = await _dashboardService.GetUnifiedDashboardAsync(tenantId, circleCode, timeFilter, startDate, endDate);
            return Ok(metrics);
        }

        /// <summary>
        /// f) Gets metrics for a specific circle (drill-down)
        /// </summary>
        [HttpGet("circle/{circleCode}")]
        public async Task<ActionResult<CircleMetricsDto>> GetCircleMetrics(string circleCode)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;

            // Circle heads can only see their own circle
            if (User.IsInRole("CircleHead"))
            {
                var userCircle = User.FindFirst("circleCode")?.Value;
                if (!string.Equals(circleCode, userCircle, StringComparison.OrdinalIgnoreCase))
                    return Forbid("You can only view metrics for your assigned circle");
            }

            var metrics = await _dashboardService.GetCircleMetricsAsync(circleCode, tenantId);
            return Ok(metrics);
        }

        /// <summary>
        /// a) Gets 802.1x machine details
        /// </summary>
        [HttpGet("machines/802.1x")]
        public async Task<ActionResult<List<DeviceDetailsDto>>> Get802_1xMachines(
            [FromQuery] string healthStatus = null,
            [FromQuery] int take = 500)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var circleCode = User.IsInRole("CircleHead")
                ? User.FindFirst("circleCode")?.Value
                : null;

            var devices = await _dashboardService.GetDeviceDetailsAsync(
                tenantId, circleCode, authType: "802.1x", healthStatus, machinesOnly: true, take);

            return Ok(devices);
        }

        /// <summary>
        /// b) Gets MAB machine details
        /// </summary>
        [HttpGet("machines/mab")]
        public async Task<ActionResult<List<DeviceDetailsDto>>> GetMABMachines(
            [FromQuery] string healthStatus = null,
            [FromQuery] int take = 500)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var circleCode = User.IsInRole("CircleHead")
                ? User.FindFirst("circleCode")?.Value
                : null;

            var devices = await _dashboardService.GetDeviceDetailsAsync(
                tenantId, circleCode, authType: "MAB", healthStatus, machinesOnly: false, take);

            return Ok(devices);
        }

        /// <summary>
        /// c) Gets healthy machine details
        /// </summary>
        [HttpGet("machines/healthy")]
        public async Task<ActionResult<List<DeviceDetailsDto>>> GetHealthyMachines(
            [FromQuery] string authType = null,
            [FromQuery] int take = 500)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var circleCode = User.IsInRole("CircleHead")
                ? User.FindFirst("circleCode")?.Value
                : null;

            var devices = await _dashboardService.GetDeviceDetailsAsync(
                tenantId, circleCode, authType, healthStatus: "Healthy", machinesOnly: false, take);

            return Ok(devices);
        }

        /// <summary>
        /// d) Gets unhealthy machine details
        /// </summary>
        [HttpGet("machines/unhealthy")]
        public async Task<ActionResult<List<DeviceDetailsDto>>> GetUnhealthyMachines(
            [FromQuery] string authType = null,
            [FromQuery] int take = 500)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var circleCode = User.IsInRole("CircleHead")
                ? User.FindFirst("circleCode")?.Value
                : null;

            var devices = await _dashboardService.GetDeviceDetailsAsync(
                tenantId, circleCode, authType, healthStatus: "Unhealthy", machinesOnly: false, take);

            return Ok(devices);
        }

        /// <summary>
        /// Gets all machine details with flexible filtering
        /// </summary>
        [HttpGet("machines")]
        public async Task<ActionResult<List<DeviceDetailsDto>>> GetMachines(
            [FromQuery] string authType = null,
            [FromQuery] string healthStatus = null,
            [FromQuery] bool machinesOnly = false,
            [FromQuery] int take = 500)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var circleCode = User.IsInRole("CircleHead")
                ? User.FindFirst("circleCode")?.Value
                : null;

            var devices = await _dashboardService.GetDeviceDetailsAsync(
                tenantId, circleCode, authType, healthStatus, machinesOnly, take);

            return Ok(devices);
        }

        /// <summary>
        /// Gets quarantine details categorized by issue type (AV, AD, Other)
        /// As per SBI requirements for quarantine reporting
        /// </summary>
        [HttpGet("quarantine")]
        public async Task<ActionResult<QuarantineDetailsDto>> GetQuarantineDetails([FromQuery] int avPage = 1,[FromQuery] int adPage = 1,[FromQuery] int otherPage = 1,[FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,[FromQuery] List<string> avReason = null,[FromQuery] List<string> adReason = null,[FromQuery] List<string> otherReason = null, [FromQuery] string? circleCode = null, [FromQuery] string? timeFilter = "custom", [FromQuery] DateTime? startDate = null,[FromQuery] DateTime? endDate = null, [FromQuery] bool unique = false)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var circle = !string.IsNullOrEmpty(circleCode) && circleCode != "All"
         ? circleCode
         : User.IsInRole("CircleHead")
             ? User.FindFirst("circleCode")?.Value
             : null;

            var result = await _dashboardService.GetQuarantineDetailsAsync(
                tenantId, circle,
                avPage, adPage, otherPage,
                pageSize, search,
                avReason, adReason, otherReason,timeFilter, startDate, endDate, unique);

            return Ok(result);
        }

        /// <summary>
        /// g) Gets comparative (historical) metrics for last 7 days
        /// Supports trend analysis and outlook
        /// </summary>
        [HttpGet("comparative")]
        public async Task<ActionResult<ComparativeMetricsDto>> GetComparativeMetrics(
            [FromQuery] int days = 7)
        {
            if (days <= 0) days = 7;
            if (days > 90) days = 90;

            var tenantId = User.FindFirst("tenantId")?.Value;
            var circleCode = User.IsInRole("CircleHead")
                ? User.FindFirst("circleCode")?.Value
                : null;

            var metrics = await _dashboardService.GetComparativeMetricsAsync(days, tenantId, circleCode);
            return Ok(metrics);
        }

        /// <summary>
        /// Export dashboard data to CSV format
        /// As per SBI requirement for Excel 2013 (.CSV) export
        /// </summary>
        [HttpGet("export/csv")]
        public async Task<IActionResult> ExportToCSV(
            [FromQuery] string dataType = "devices") // devices, quarantine, historical
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var circleCode = User.IsInRole("CircleHead")
                ? User.FindFirst("circleCode")?.Value
                : null;

            switch (dataType.ToLower())
            {
                case "devices":
                    var devices = await _dashboardService.GetDeviceDetailsAsync(
                        tenantId, circleCode, take: 10000);
                    return File(GenerateDeviceCSV(devices), "text/csv",
                        $"devices_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                case "quarantine":
                    var quarantine = await _dashboardService.GetQuarantineDetailsAsync(tenantId, circleCode);
                    return File(GenerateQuarantineCSV(quarantine), "text/csv",
                        $"quarantine_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                case "historical":
                    var historical = await _dashboardService.GetComparativeMetricsAsync(7, tenantId, circleCode);
                    return File(GenerateHistoricalCSV(historical), "text/csv",
                        $"historical_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                default:
                    return BadRequest("Invalid dataType. Use: devices, quarantine, or historical");
            }
        }

        [HttpGet("quarantine/export")]
        public async Task<IActionResult> ExportQuarantine(
    [FromQuery] string category,
    [FromQuery] string? circleCode,
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    [FromQuery] string? search,
    [FromQuery] List<string>? adReason,
    [FromQuery] List<string>? otherReason,
    [FromQuery] List<string>? avReason,
    [FromQuery] bool unique = false)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var circle = !string.IsNullOrEmpty(circleCode) && circleCode != "All"
                ? circleCode
                : User.IsInRole("CircleHead")
                    ? User.FindFirst("circleCode")?.Value
                    : null;

            var devices = await _dashboardService.GetQuarantineDevicesForExport(
                tenantId, category, circle, search, avReason, adReason, otherReason, startDate, endDate, unique);

            var csvBytes = category switch
            {
                "AD" => GenerateAdCsv(devices),
                "Other" => GenerateOtherCsv(devices),
                "AV" => GenerateAvCsv(devices),
                _ => throw new ArgumentException("Invalid category")
            };

            var fileName = $"{category}_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(csvBytes, "text/csv", fileName);
        }

        [HttpGet("nas-devices/export")]
        public async Task<IActionResult> ExportNasDevices(
            [FromQuery] string? circleCode,
            [FromQuery] bool? activeOnly,
            [FromQuery] string? search,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var tenantId = User.FindFirst("tenantId")?.Value;

                // Same circle handling as the quarantine export
                var circle = !string.IsNullOrEmpty(circleCode) && circleCode != "All"
                    ? circleCode
                    : User.IsInRole("CircleHead")
                        ? User.FindFirst("circleCode")?.Value
                        : null;

                var devices = await _dashboardService.GetNasDevicesForExport(
                    tenantId, circle, activeOnly, search, startDate, endDate);

                var sb = new StringBuilder();
                sb.AppendLine("NAS IP,Source System,Event Type,Circle,Status,Last Seen");

                foreach (var d in devices)
                {
                    sb.AppendLine(string.Join(",",
                        Csv(d.NasIp),
                        Csv(d.SourceSystem),
                        Csv(d.EventType),
                        Csv(d.Circle),
                        Csv(d.Status),
                        Csv(d.LastSeen.ToString("dd/MM/yyyy, HH:mm:ss"))));
                }

                // UTF-8 BOM so Excel opens it correctly
                var bytes = new UTF8Encoding(true).GetPreamble()
                    .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
                    .ToArray();

                return File(bytes, "text/csv", $"nas-devices_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            // Guard against CSV/formula injection when opened in Excel
            if ("=+-@".Contains(value[0])) value = "'" + value;
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        #region CSV Generation Helpers

        private byte[] GenerateAdCsv(List<DeviceDetailsDto> devices)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("MAC Address,Username,AuthType,Circle,Login Status,Reason,Timestamp,Status");

            foreach (var d in devices)
            {
                csv.AppendLine($"{EscapeCSV(d.MacAddress)},{EscapeCSV(d.Username)}," +
                               $"{EscapeCSV(d.AuthType)},{EscapeCSV(d.CircleCode)}," +
                               $"{EscapeCSV(d.Status)},{EscapeCSV(d.QuarantineReason ?? d.QuarantineCategory)}," +
                               $"{d.EventTime:yyyy-MM-dd HH:mm:ss},{EscapeCSV(d.HealthStatus)}");
            }
            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] GenerateOtherCsv(List<DeviceDetailsDto> devices)
        {
            // Same shape as AD table (Others tab reuses ADReportTable's columns)
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("MAC Address,Username,AuthType,Circle,Login Status,Reason,Timestamp,Status");

            foreach (var d in devices)
            {
                csv.AppendLine($"{EscapeCSV(d.MacAddress)},{EscapeCSV(d.Username)}," +
                               $"{EscapeCSV(d.AuthType)},{EscapeCSV(d.CircleCode)}," +
                               $"{EscapeCSV(d.Status)},{EscapeCSV(d.QuarantineReason ?? d.QuarantineCategory)}," +
                               $"{d.EventTime:yyyy-MM-dd HH:mm:ss},{EscapeCSV(d.HealthStatus)}");
            }
            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] GenerateAvCsv(List<DeviceDetailsDto> devices)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("MAC Address,IP Address,Hostname,Circle,Reason,Timestamp,Status");

            foreach (var d in devices)
            {
                csv.AppendLine($"{EscapeCSV(d.MacAddress)},{EscapeCSV(d.IpAddress)}," +
                               $"{EscapeCSV(d.Hostname)},{EscapeCSV(d.CircleCode)}," +
                               $"{EscapeCSV(d.QuarantineReason ?? d.QuarantineCategory)}," +
                               $"{d.EventTime:yyyy-MM-dd HH:mm:ss},{EscapeCSV(d.Status)}");
            }
            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }
        private byte[] GenerateDeviceCSV(List<DeviceDetailsDto> devices)
        {
            var csv = new System.Text.StringBuilder();

            // Header
            csv.AppendLine("MAC Address,Hostname,Username,IP Address,NAS IP,Auth Type,Auth Method,Health Status,Circle Code,Branch Code,Event Time,Quarantine Reason,AV Installed,AD User Found,Compliant");

            // Data rows
            foreach (var device in devices)
            {
                csv.AppendLine($"{EscapeCSV(device.MacAddress)}," +
                              $"{EscapeCSV(device.Hostname)}," +
                              $"{EscapeCSV(device.Username)}," +
                              $"{EscapeCSV(device.IpAddress)}," +
                              $"{EscapeCSV(device.NasIp)}," +
                              $"{EscapeCSV(device.AuthType)}," +
                              $"{EscapeCSV(device.AuthMethod)}," +
                              $"{EscapeCSV(device.HealthStatus)}," +
                              $"{EscapeCSV(device.CircleCode)}," +
                              $"{EscapeCSV(device.BranchCode)}," +
                              $"{device.EventTime:yyyy-MM-dd HH:mm:ss}," +
                              $"{EscapeCSV(device.QuarantineReason)}," +
                              $"{device.AVInstalled}," +
                              $"{device.ADUserFound}," +
                              $"{device.IsCompliant}");
            }

            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] GenerateQuarantineCSV(QuarantineDetailsDto quarantine)
        {
            var csv = new System.Text.StringBuilder();

            // Header
            csv.AppendLine("Category,MAC Address,Hostname,Username,Circle Code,Quarantine Reason,Event Time");

            // AV Issues
            foreach (var device in quarantine.AVIssues?.Devices?.Items ?? new List<DeviceDetailsDto>())
            {
                csv.AppendLine($"AV Issue,{EscapeCSV(device.MacAddress)}," +
                              $"{EscapeCSV(device.Hostname)}," +
                              $"{EscapeCSV(device.Username)}," +
                              $"{EscapeCSV(device.CircleCode)}," +
                              $"{EscapeCSV(device.QuarantineReason)}," +
                              $"{device.EventTime:yyyy-MM-dd HH:mm:ss}");
            }

            // AD Issues
            foreach (var device in quarantine.ADIssues?.Devices?.Items ?? new List<DeviceDetailsDto>())
            {
                csv.AppendLine($"AD Issue,{EscapeCSV(device.MacAddress)}," +
                              $"{EscapeCSV(device.Hostname)}," +
                              $"{EscapeCSV(device.Username)}," +
                              $"{EscapeCSV(device.CircleCode)}," +
                              $"{EscapeCSV(device.QuarantineReason)}," +
                              $"{device.EventTime:yyyy-MM-dd HH:mm:ss}");
            }

            // Other Issues
            foreach (var device in quarantine.OtherIssues?.Devices?.Items ?? new List<DeviceDetailsDto>())
            {
                csv.AppendLine($"Other Issue,{EscapeCSV(device.MacAddress)}," +
                              $"{EscapeCSV(device.Hostname)}," +
                              $"{EscapeCSV(device.Username)}," +
                              $"{EscapeCSV(device.CircleCode)}," +
                              $"{EscapeCSV(device.QuarantineReason)}," +
                              $"{device.EventTime:yyyy-MM-dd HH:mm:ss}");
            }

            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] GenerateHistoricalCSV(ComparativeMetricsDto comparative)
        {
            var csv = new System.Text.StringBuilder();

            // Header
            csv.AppendLine("Date,802.1x Machines,MAB Machines,Healthy Machines,Unhealthy Machines,Quarantined,Isolated,Health %");

            // Data rows
            foreach (var metric in comparative.DailyMetrics)
            {
                csv.AppendLine($"{metric.Date:yyyy-MM-dd}," +
                              $"{metric.Total802_1xMachines}," +
                              $"{metric.TotalMABMachines}," +
                              $"{metric.HealthyMachines}," +
                              $"{metric.UnhealthyMachines}," +
                              $"{metric.QuarantinedDevices}," +
                              $"{metric.IsolatedDevices}," +
                              $"{metric.HealthPercentage:F2}");
            }

            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }

        private string EscapeCSV(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // Escape double quotes and wrap in quotes if contains comma, quote, or newline
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }

        #endregion
    }
}