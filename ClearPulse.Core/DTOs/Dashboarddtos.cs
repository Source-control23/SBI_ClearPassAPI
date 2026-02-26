namespace ClearPulse.Core.DTOs
{
    /// <summary>
    /// Unified dashboard metrics for Corporate view (all circles)
    /// </summary>
    public class DashboardMetricsDto
    {
        public DateTime LastUpdated { get; set; }
        public string TenantId { get; set; }

        // a) 802.1x Machines
        public int Total802_1xMachines { get; set; }

        // b) MAB Machines
        public int TotalMABMachines { get; set; }

        // c) Healthy Machines
        public int TotalHealthyMachines { get; set; }

        // d) Unhealthy Machines
        public int TotalUnhealthyMachines { get; set; }

        // e) NAS Devices
        public NASMetrics NASMetrics { get; set; }

        // h) Cluster Status
        public ClusterMetrics ClusterMetrics { get; set; }

        // i) AD Integration Status
        public ADIntegrationMetrics ADMetrics { get; set; }

        public List<DeviceDetailsDto> deviceDetails { get; set; }
        // f) Circle-wise breakdown
        public List<CircleMetricsDto> CircleBreakdown { get; set; }
    }

    /// <summary>
    /// Circle-specific metrics for drill-down
    /// </summary>
    public class CircleMetricsDto
    {
        public string CircleCode { get; set; }
        public string CircleName { get; set; }
        public int Total802_1xMachines { get; set; }
        public int TotalMABMachines { get; set; }
        public int TotalHealthyMachines { get; set; }
        public int TotalUnhealthyMachines { get; set; }
        public int TotalDevices { get; set; }
        public double HealthPercentage { get; set; }
    }

    public class NASMetrics
    {
        public int TotalNASDevices { get; set; }
        public int ActiveNASDevices { get; set; }
        public int InactiveNASDevices { get; set; }
        public List<NASDeviceDto> Details { get; set; }
    }

    public class NASDeviceDto
    {
        public string NasIp { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public int RequestCount { get; set; }
        public DateTime LastSeen { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; }
    }

    public class ClusterMetrics
    {
        public int TotalNodes { get; set; }
        public int HealthyNodes { get; set; }
        public int DegradedNodes { get; set; }
        public string OverallStatus { get; set; }
        public List<ClusterNodeDto> Nodes { get; set; }
    }

    public class ClusterNodeDto
    {
        public string NodeIp { get; set; }
        public string NodeName { get; set; }
        public string NodeRole { get; set; }
        public string SyncStatus { get; set; }
        public string ReplicationStatus { get; set; }
        public string Uptime { get; set; }
        public float CpuUsage { get; set; }
        public float MemoryUsage { get; set; }
        public DateTime EventTime { get; set; }
    }

    public class ADIntegrationMetrics
    {
        public int TotalADEvents { get; set; }
        public int SuccessfulEvents { get; set; }
        public int FailedEvents { get; set; }
        public double SuccessRate { get; set; }
        public List<ADIntegrationDto> RecentFailures { get; set; }
    }

    public class ADIntegrationDto
    {
        public string AdServer { get; set; }
        public string DomainName { get; set; }
        public string FailureReason { get; set; }
        public int? ErrorCode { get; set; }
        public string Status { get; set; }
        public DateTime EventTime { get; set; }
        public string CircleCode { get; set; }
    }

    /// <summary>
    /// Device details for drill-down views
    /// </summary>
    public class DeviceDetailsDto
    {
        public string MacAddress { get; set; }
        public string Hostname { get; set; }
        public string Username { get; set; }
        public string IpAddress { get; set; }
        public string NasIp { get; set; }
        public string AuthType { get; set; }
        public string AuthMethod { get; set; }
        public string HealthStatus { get; set; }
        public string Status { get; set; }
        public string CircleCode { get; set; }
        public string BranchCode { get; set; }
        public DateTime EventTime { get; set; }
        public DateTime? LastAuthSuccess { get; set; }
        public DateTime? LastAuthFailure { get; set; }

        // Quarantine details
        public string QuarantineCategory { get; set; }
        public string QuarantineReason { get; set; }
        public string IsolationState { get; set; }

        // AV Status
        public bool AVInstalled { get; set; }
        public bool AVUpdated { get; set; }
        public bool AVScanned { get; set; }
        public string AVProduct { get; set; }

        // AD Status
        public bool ADUserFound { get; set; }
        public bool ADAccountDisabled { get; set; }
        public string ADDomain { get; set; }

        // Compliance
        public bool IsCompliant { get; set; }
        public string ComplianceIssues { get; set; }
    }

    /// <summary>
    /// g) Historical metrics for trend analysis
    /// </summary>
    public class HistoricalMetricsDto
    {
        public DateTime Date { get; set; }
        public int Total802_1xMachines { get; set; }
        public int TotalMABMachines { get; set; }
        public int HealthyMachines { get; set; }
        public int UnhealthyMachines { get; set; }
        public int QuarantinedDevices { get; set; }
        public int IsolatedDevices { get; set; }
        public double HealthPercentage { get; set; }
    }

    /// <summary>
    /// Comparative metrics for 7-day trend
    /// </summary>
    public class ComparativeMetricsDto
    {
        public List<HistoricalMetricsDto> DailyMetrics { get; set; }
        public TrendSummary Trends { get; set; }
    }

    public class TrendSummary
    {
        public int HealthyDeviceTrend { get; set; } // +/- change from 7 days ago
        public int UnhealthyDeviceTrend { get; set; }
        public double HealthPercentageChange { get; set; }
        public string Outlook { get; set; } // "Improving", "Stable", "Degrading"
    }

    /// <summary>
    /// Quarantine details by category
    /// </summary>
    public class QuarantineDetailsDto
    {
        public AVQuarantineMetrics AVIssues { get; set; }
        public ADQuarantineMetrics ADIssues { get; set; }
        public OtherQuarantineMetrics OtherIssues { get; set; }
    }

    public class AVQuarantineMetrics
    {
        public int NotInstalled { get; set; }
        public int NotUpdated { get; set; }
        public int NotScanned { get; set; }
        public PagedResult<DeviceDetailsDto> Devices { get; set; }
        public List<string> AvailableReasons { get; set; }
    }

    public class ADQuarantineMetrics
    {
        public int AccountDisabled { get; set; }
        public int UserNotFound { get; set; }
        public int CertificateInvalid { get; set; }
        public PagedResult<DeviceDetailsDto> Devices { get; set; }
        public List<string> AvailableReasons { get; set; }
    }

    public class OtherQuarantineMetrics
    {
        public int AuthenticationFailed { get; set; }
        public int Other { get; set; }
        public PagedResult<DeviceDetailsDto> Devices { get; set; }
        public List<string> AvailableReasons { get; set; }

    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}