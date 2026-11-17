using Azure;
using ClearPulse.Core.DTOs;
using ClearPulse.Core.Entities;
using ClearPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq.Expressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace ClearPulse.Infrastructure.Services
{
    public interface IDashboardService
    {
        Task<DashboardMetricsDto> GetUnifiedDashboardAsync(string tenantId, string circleCode, string timeFilter, DateTime? startDate = null, DateTime? endDate = null);
        Task<CircleMetricsDto> GetCircleMetricsAsync(string circleCode, string tenantId);
        Task<List<DeviceDetailsDto>> GetDeviceDetailsAsync(string tenantId, string circleCode,
            string authType = null, string healthStatus = null, bool machinesOnly = false, int take = 500, string timeFilter = "custom", DateTime? startDate = null, DateTime? endDate = null);
        //Task<QuarantineDetailsDto> GetQuarantineDetailsAsync(string tenantId, string circleCode = null);
        Task<QuarantineDetailsDto> GetQuarantineDetailsAsync(
               string tenantId,
               string circleCode = null,
               int avPage = 1,
               int adPage = 1,
               int otherPage = 1,
               int pageSize = 10,
               string search = null,
               List<string> avReason = null,
               List<string> adReason = null,
               List<string> otherReason = null,
               string timeFilter = "custom",
               DateTime? startDate = null,
               DateTime? endDate = null, bool unique = false);

        Task<List<DeviceDetailsDto>> GetQuarantineDevicesForExport(
     string tenantId,
     string category,
     string circleCode = null,
     string search = null,
     List<string> avReason = null,
     List<string> adReason = null,
     List<string> otherReason = null,
     DateTime? startDate = null,
     DateTime? endDate = null,
     bool unique = false);

        Task<List<NASDeviceDto>> GetNasDevicesForExport(
     string? tenantId,
     string? circleCode = null,
     bool? activeOnly = null,       // null = All, true = Active, false = Inactive
     string? search = null,
     DateTime? startDate = null,
     DateTime? endDate = null);

        Task<ComparativeMetricsDto> GetComparativeMetricsAsync(int days, string tenantId, string circleCode = null);
    }

    public class DashboardService : IDashboardService
    {
        private readonly ClearPulseDbContext _db;

        public DashboardService(ClearPulseDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Gets unified dashboard with all metrics
        /// Supports both Corporate (all circles) and Circle Head (specific circle) views
        /// </summary>
        //public async Task<DashboardMetricsDto> GetUnifiedDashboardAsync(string tenantId, string circleCode, string timeFilter, DateTime? startDate = null, DateTime? endDate = null)
        //{
        //    // Base query for device auth events
        //    var baseQuery = _db.DeviceAuthEvents.AsQueryable();

        //    if (!string.IsNullOrEmpty(tenantId))
        //        baseQuery = baseQuery.Where(d => d.TenantId == tenantId);

        //    if (!string.IsNullOrEmpty(circleCode))
        //        baseQuery = baseQuery.Where(d => d.CircleCode == circleCode);
        //    // CUSTOM DATE FILTER - LAST 7 DAYS
        //    if (!string.Equals(timeFilter, "custom", StringComparison.OrdinalIgnoreCase))
        //    {
        //        throw new ArgumentException(
        //            "Only custom date filter is supported.");
        //    }

        //    if (!startDate.HasValue || !endDate.HasValue)
        //    {
        //        throw new ArgumentException(
        //            "Start date and end date are required.");
        //    }

        //    var requestedStartDate = startDate.Value.Date;
        //    var requestedEndDate = endDate.Value.Date;

        //    if (requestedStartDate > requestedEndDate)
        //    {
        //        throw new ArgumentException(
        //            "Start date cannot be greater than end date.");
        //    }
        //    var today = DateTime.Today;

        //    // Today + previous 6 days = 7 calendar days
        //    var minimumAllowedDate = today.AddDays(-6);

        //    if (requestedStartDate < minimumAllowedDate)
        //    {
        //        throw new ArgumentException(
        //            "Custom date range can only include the last 7 days.");
        //    }

        //    if (requestedEndDate > today)
        //    {
        //        throw new ArgumentException(
        //            "End date cannot be in the future.");
        //    }

        //    var customStartDate = requestedStartDate;

        //    // End date is inclusive
        //    // Example:
        //    // 17 Aug -> < 18 Aug
        //    var customEndDateExclusive = requestedEndDate.AddDays(1);

        //    baseQuery = baseQuery.Where(x =>
        //        x.EventTime >= customStartDate &&
        //        x.EventTime < customEndDateExclusive);

        //    // Get latest event per device (by MAC address)
        //    var latestDevices = await baseQuery
        //        .AsNoTracking()
        //        .GroupBy(d => d.MacAddress)
        //        .Select(g => g.OrderByDescending(d => d.EventTime).FirstOrDefault())
        //        .ToListAsync();

        //    // a) Total 802.1x machines
        //    var total802_1x = latestDevices.Count(d => d.AuthType == "802.1x" &&
        //                                    d.HealthStatus == "Healthy");

        //    // b) Total MAB machines
        //    var totalMAB = latestDevices.Count(d => d.AuthType == "MAB" &&
        //                                 d.HealthStatus == "Healthy");

        //    // c) Total healthy machines
        //    var totalHealthy = latestDevices.Count(d => d.HealthStatus == "Healthy");

        //    // d) Total unhealthy machines
        //    var totalUnhealthy = latestDevices.Count(d => d.HealthStatus == "Unhealthy");

        //    // e) NAS metrics
        //    var nasMetrics = await GetNASMetricsAsync(tenantId, circleCode , timeFilter, startDate, endDate);

        //    // h) Cluster metrics
        //    //var clusterMetrics = await GetClusterMetricsAsync(tenantId , timeFilter);

        //    // i) AD integration metrics
        //    //var adMetrics = await GetADIntegrationMetricsAsync(tenantId, circleCode , timeFilter);

        //    // AD / AV 
        //    var quarantine = await GetQuarantineDetailsAsync(tenantId,circleCode, 1,1,1, 10,null,null,null,null,timeFilter, startDate, endDate);

        //    var devicesMetrics = await GetDeviceDetailsAsync(tenantId, circleCode, null, null, false, 500, timeFilter, startDate, endDate);

        //    // f) Circle-wise breakdown
        //    var circleBreakdown = await GetCircleBreakdownAsync(tenantId , timeFilter, startDate, endDate);

        //    return new DashboardMetricsDto
        //    {
        //        LastUpdated = DateTime.UtcNow,
        //        TenantId = tenantId,
        //        Total802_1xMachines = total802_1x,
        //        TotalMABMachines = totalMAB,
        //        TotalHealthyMachines = totalHealthy,
        //        TotalUnhealthyMachines = totalUnhealthy,
        //        NASMetrics = nasMetrics,
        //        //ClusterMetrics = clusterMetrics,
        //        deviceDetails = devicesMetrics,
        //        //ADMetrics = adMetrics,
        //        CircleBreakdown = circleBreakdown,s
        //        Quarantine = quarantine
        //    };
        //}

        public async Task<DashboardMetricsDto> GetUnifiedDashboardAsync(
    string tenantId,
    string circleCode,
    string timeFilter,
    DateTime? startDate = null,
    DateTime? endDate = null)
        {
            // Always use custom date filtering.
            // If no date is selected from UI, default to today.
            timeFilter = "custom";

            var today = DateTime.Today;
            var minimumAllowedDate = today.AddDays(-6);

            // If dates are not supplied, use TODAY.
            var requestedStartDate = startDate?.Date ?? today;
            var requestedEndDate = endDate?.Date ?? today;

            // Validate date range
            if (requestedStartDate > requestedEndDate)
            {
                throw new ArgumentException(
                    "Start date cannot be greater than end date.");
            }

            if (requestedStartDate < minimumAllowedDate)
            {
                throw new ArgumentException(
                    "Custom date range can only include the last 7 days.");
            }

            if (requestedEndDate > today)
            {
                throw new ArgumentException(
                    "End date cannot be in the future.");
            }

            // End date is inclusive
            var customEndDateExclusive = requestedEndDate.AddDays(1);

            var baseQuery = _db.DeviceAuthEvents
                .AsNoTracking();

            if (!string.IsNullOrEmpty(tenantId))
                baseQuery = baseQuery.Where(d => d.TenantId == tenantId);

            if (!string.IsNullOrEmpty(circleCode))
                baseQuery = baseQuery.Where(d => d.CircleCode == circleCode);

            // Apply SAME date range
            baseQuery = baseQuery.Where(x =>
                x.EventTime >= requestedStartDate &&
                x.EventTime < customEndDateExclusive);

            // ------------------------------------
            // SUCCESSFUL AUTHENTICATION DEVICES
            // ------------------------------------

            var successfulAuthQuery = baseQuery
                .Where(d => d.Status == "ACCEPT");
            var sw = Stopwatch.StartNew();

            Console.WriteLine($"Connected devices: {sw.ElapsedMilliseconds} ms");

            sw.Restart();

            // ------------------------------------
            // LAST SUCCESSFUL AUTHENTICATION
            // ------------------------------------

            var lastUpdated = await successfulAuthQuery
                .MaxAsync(d => (DateTime?)d.EventTime);
            Console.WriteLine($"Last updated: {sw.ElapsedMilliseconds} ms");

            sw.Restart();

            //var latestDevices = await baseQuery
            //    .GroupBy(d => d.MacAddress)
            //    .Select(g =>
            //        g.OrderByDescending(d => d.EventTime)
            //         .FirstOrDefault())
            //    .ToListAsync();

            var latestDevices = await baseQuery.Where(d => !string.IsNullOrEmpty(d.MacAddress)).GroupBy(d => d.MacAddress)
    .Select(g => g.OrderByDescending(d => d.EventTime).Select(d => new
    {
        d.Id,
        d.MacAddress,
        d.EventTime,
        d.AuthType,
        d.HealthStatus,
        d.Status
    }).FirstOrDefault()).ToListAsync();

            // Connected = latest event for the device is ACCEPT and Healthy
            var totalConnectedDevices = latestDevices.Count(d =>
                d.Status == "ACCEPT" &&
                d.HealthStatus == "Healthy");

            var total802_1x = latestDevices.Count(d =>
                d.AuthType == "802.1x" &&
                d.HealthStatus == "Healthy");

            var totalMAB = latestDevices.Count(d =>
                d.AuthType == "MAB" &&
                d.HealthStatus == "Healthy");

            var totalHealthy = latestDevices.Count(d =>
                d.HealthStatus == "Healthy");

            var totalUnhealthy = latestDevices.Count(d =>
                d.HealthStatus == "Unhealthy");

            Console.WriteLine($"latestDevices: {sw.ElapsedMilliseconds} ms");

            sw.Restart();

            var nasMetrics = await GetNASMetricsAsync(
                tenantId,
                circleCode,
                "custom",
                requestedStartDate,
                requestedEndDate);

            Console.WriteLine($"NAS: {sw.ElapsedMilliseconds} ms");

            sw.Restart();

            var quarantine = await GetQuarantineDetailsAsync(
                tenantId,
                circleCode,
                1,
                1,
                1,
                10,
                null,
                null,
                null,
                null,
                "custom",
                requestedStartDate,
                requestedEndDate ,true);
            Console.WriteLine($"Quarantine: {sw.ElapsedMilliseconds} ms");

            sw.Restart();

            var devicesMetrics = await GetDeviceDetailsAsync(
                tenantId,
                circleCode,
                null,
                null,
                false,
                500,
                "custom",
                requestedStartDate,
                requestedEndDate);
            Console.WriteLine($"Devices: {sw.ElapsedMilliseconds} ms");

            sw.Restart();

            var circleBreakdown = await GetCircleBreakdownAsync(
                tenantId,
                "custom",
                requestedStartDate,
                requestedEndDate);
            Console.WriteLine($"Circle: {sw.ElapsedMilliseconds} ms");

            return new DashboardMetricsDto
            {
                LastUpdated = lastUpdated,
                TenantId = tenantId,    
                TotalConnectedDevices = totalConnectedDevices,
                Total802_1xMachines = total802_1x,
                TotalMABMachines = totalMAB,
                TotalHealthyMachines = totalHealthy,
                TotalUnhealthyMachines = totalUnhealthy,
                NASMetrics = nasMetrics,
                deviceDetails = devicesMetrics,
                CircleBreakdown = circleBreakdown,
                Quarantine = quarantine
            };
        }

        /// <summary>
        /// Gets metrics for a specific circle
        /// </summary>
        public async Task<CircleMetricsDto> GetCircleMetricsAsync(string circleCode, string tenantId)
        {
            var baseQuery = _db.DeviceAuthEvents
                .Where(d => d.CircleCode == circleCode);

            if (!string.IsNullOrEmpty(tenantId))
                baseQuery = baseQuery.Where(d => d.TenantId == tenantId);

            var latestDevices = await baseQuery
                .GroupBy(d => d.MacAddress)
                .Select(g => g.OrderByDescending(d => d.EventTime).FirstOrDefault())
                .ToListAsync();

            var total802_1x = latestDevices.Count(d => d.AuthType == "802.1x" &&
                                                        d.Username != null &&
                                                        d.Username.StartsWith("host/"));
            var totalMAB = latestDevices.Count(d => d.AuthType == "MAB");
            var totalHealthy = latestDevices.Count(d => d.HealthStatus == "Healthy");
            var totalUnhealthy = latestDevices.Count(d => d.HealthStatus == "Unhealthy");
            var totalDevices = latestDevices.Count;

            return new CircleMetricsDto
            {
                CircleCode = circleCode,
                CircleName = await GetCircleName(circleCode),
                Total802_1xMachines = total802_1x,
                TotalMABMachines = totalMAB,
                TotalHealthyMachines = totalHealthy,
                TotalUnhealthyMachines = totalUnhealthy,
                TotalDevices = totalDevices,
                HealthPercentage = totalDevices > 0 ? (totalHealthy * 100.0 / totalDevices) : 0
            };
        }

        /// <summary>
        /// Gets detailed device list with filters
        /// </summary>
        public async Task<List<DeviceDetailsDto>> GetDeviceDetailsAsync(
            string tenantId,
            string circleCode,
            string authType = null,
            string healthStatus = null,
            bool machinesOnly = false,
            int take = 500,
             string timeFilter = "custom",
             DateTime? startDate = null,
             DateTime? endDate = null)
        {
            if (take <= 0) take = 100;
            if (take > 2000) take = 2000;

            // Use DeviceInventory for current state
            var query = _db.DeviceInventory.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                query = query.Where(d => d.TenantId == tenantId);

            if (!string.IsNullOrEmpty(circleCode))
                query = query.Where(d => d.CircleCode == circleCode);

            // Date filter
            if (!string.Equals(timeFilter, "custom",
                StringComparison.OrdinalIgnoreCase))
            {
                timeFilter = "custom";
            }

            // If no date is selected, use today
            var today = DateTime.Today;

            var customStartDate = startDate?.Date ?? today;
            var customEndDate = endDate?.Date ?? today;

            var minimumAllowedDate = today.AddDays(-6);

            if (customStartDate > customEndDate)
            {
                throw new ArgumentException(
                    "Start date cannot be greater than end date.");
            }

            if (customStartDate < minimumAllowedDate)
            {
                throw new ArgumentException(
                    "Date range can only include the last 7 days.");
            }

            if (customEndDate > today)
            {
                throw new ArgumentException(
                    "End date cannot be in the future.");
            }

            var exclusiveEndDate = customEndDate.AddDays(1);

            query = query.Where(d =>
                d.LastSeenOnline >= customStartDate &&
                d.LastSeenOnline < exclusiveEndDate);

            // Get matching devices from inventory
            var devices = await query
                .OrderByDescending(d => d.LastSeenOnline)
                .Take(take)
                .Select(d => new DeviceDetailsDto
                {
                    MacAddress = d.MacAddress,
                    Hostname = d.Hostname,
                    IpAddress = d.IpAddress,
                    HealthStatus = d.CurrentHealthStatus,
                    CircleCode = d.CircleCode,
                    BranchCode = d.BranchCode,
                    LastAuthSuccess = d.LastAuthSuccess,
                    LastAuthFailure = d.LastAuthFailure,
                    AVInstalled = d.AVInstalled ?? false,
                    AVProduct = d.AVProduct,
                    IsCompliant = d.IsCompliant ?? false,
                    ComplianceIssues = d.ComplianceIssues
                })
                .ToListAsync();

            // Enrich with latest auth event data
            var macAddresses = devices.Select(d => d.MacAddress).ToList();
            var authEventQuery = _db.DeviceAuthEvents
     .Where(e => macAddresses.Contains(e.MacAddress));

            authEventQuery = authEventQuery.Where(e =>
                e.EventTime >= customStartDate &&
                e.EventTime < exclusiveEndDate);

            var latestAuthEvents = await authEventQuery
                .GroupBy(e => e.MacAddress)
                .Select(g =>
                    g.OrderByDescending(e => e.EventTime).FirstOrDefault())
                .ToListAsync();

            foreach (var device in devices)
            {
                var authEvent = latestAuthEvents.FirstOrDefault(e => e.MacAddress == device.MacAddress);
                if (authEvent != null)
                {
                    device.Username = authEvent.Username;
                    device.NasIp = authEvent.NasIp;
                    device.AuthType = authEvent.AuthType;
                    device.AuthMethod = authEvent.AuthMethod;
                    device.Status = authEvent.Status;
                    device.EventTime = authEvent.EventTime;
                    device.QuarantineCategory = authEvent.QuarantineCategory;
                    device.QuarantineReason = authEvent.QuarantineReason;
                    device.IsolationState = authEvent.IsolationState;
                    device.ADUserFound = authEvent.ADUserFound;
                    device.ADAccountDisabled = authEvent.ADAccountDisabled;
                    device.ADDomain = authEvent.ADDomain;
                }
            }

            // Apply filters
            if (!string.IsNullOrEmpty(authType))
                devices = devices.Where(d => d.AuthType == authType).ToList();

            if (!string.IsNullOrEmpty(healthStatus))
                devices = devices.Where(d => d.HealthStatus == healthStatus).ToList();

            if (machinesOnly)
                devices = devices.Where(d => d.Username != null && d.Username.StartsWith("host/")).ToList();

            return devices;
        }

        /// <summary>
        /// Gets quarantine details categorized by issue type
        /// </summary>
        //public async Task<QuarantineDetailsDto> GetQuarantineDetailsAsync(string tenantId, string circleCode = null)
        //{
        //    var query = _db.DeviceAuthEvents.AsQueryable();

        //    if (!string.IsNullOrEmpty(tenantId))
        //        query = query.Where(d => d.TenantId == tenantId);

        //    if (!string.IsNullOrEmpty(circleCode))
        //        query = query.Where(d => d.CircleCode == circleCode);

        //    // Get latest event per device
        //    var quarantinedDevices = await query
        //        .Where(d => d.Status == "Quarantine" || d.IsolationState == "Isolated")
        //        .GroupBy(d => d.MacAddress)
        //        .Select(g => g.OrderByDescending(d => d.EventTime).FirstOrDefault())
        //        .ToListAsync();

        //    // Categorize by issue type
        //    var avIssues = quarantinedDevices
        //        .Where(d => d.QuarantineCategory == "AV_Issue")
        //        .Select(MapToDeviceDetails)
        //        .ToList();

        //    var adIssues = quarantinedDevices
        //        .Where(d => d.QuarantineCategory == "AD_Issue")
        //        .Select(MapToDeviceDetails)
        //        .ToList();

        //    var otherIssues = quarantinedDevices
        //        .Where(d => d.QuarantineCategory == "Other" || string.IsNullOrEmpty(d.QuarantineCategory))
        //        .Select(MapToDeviceDetails)
        //        .ToList();

        //    return new QuarantineDetailsDto
        //    {
        //        AVIssues = new AVQuarantineMetrics
        //        {
        //            NotInstalled = avIssues.Count(d => !d.AVInstalled),
        //            NotUpdated = avIssues.Count(d => d.AVInstalled && !d.AVUpdated),
        //            NotScanned = avIssues.Count(d => d.AVInstalled && !d.AVScanned),
        //            Devices = avIssues
        //        },
        //        ADIssues = new ADQuarantineMetrics
        //        {
        //            AccountDisabled = adIssues.Count(d => d.ADAccountDisabled),
        //            UserNotFound = adIssues.Count(d => !d.ADUserFound),
        //            CertificateInvalid = adIssues.Count(d => d.QuarantineReason?.Contains("certificate") ?? false),
        //            Devices = adIssues
        //        },
        //        OtherIssues = new OtherQuarantineMetrics
        //        {
        //            AuthenticationFailed = otherIssues.Count(d => d.QuarantineReason?.Contains("Authentication Failed") ?? false),
        //            Other = otherIssues.Count(d => !(d.QuarantineReason?.Contains("Authentication Failed") ?? false)),
        //            Devices = otherIssues
        //        }
        //    };
        //}

        public async Task<QuarantineDetailsDto> GetQuarantineDetailsAsync(
     string tenantId,
     string circleCode = null,
     int avPage = 1,
     int adPage = 1,
     int otherPage = 1,
     int pageSize = 10,
     string search = null,
     List<string> avReason = null,
     List<string> adReason = null,
     List<string> otherReason = null,
     string timeFilter = "custom",
     DateTime? startDate = null,
     DateTime? endDate = null,
     bool unique = false)
        {
            IQueryable<DeviceAuthEvent> baseQuery = _db.DeviceAuthEvents
                .AsNoTracking()
                .Where(d =>
                    (d.Status == "UNHEALTHY" && d.QuarantineCategory == "AV_Issue") ||
                    ((d.Status == "REJECT" || d.Status == "TIMEOUT") && d.QuarantineCategory == "AD_Issue") ||
                    d.QuarantineCategory == "Other");

            if (!string.IsNullOrEmpty(tenantId))
                baseQuery = baseQuery.Where(d => d.TenantId == tenantId);

            if (!string.IsNullOrEmpty(circleCode))
                baseQuery = baseQuery.Where(d => d.CircleCode == circleCode);

            // Same date handling as GetUnifiedDashboardAsync
            var today = DateTime.Today;
            var minimumAllowedDate = today.AddDays(-6);

            var requestedStartDate = startDate?.Date ?? today;
            var requestedEndDate = endDate?.Date ?? today;

            if (requestedStartDate > requestedEndDate)
                throw new ArgumentException("Start date cannot be greater than end date.");

            if (requestedStartDate < minimumAllowedDate)
                throw new ArgumentException("Custom date range can only include the last 7 days.");

            if (requestedEndDate > today)
                throw new ArgumentException("End date cannot be in the future.");

            var requestedEndDateExclusive = requestedEndDate.AddDays(1);

            baseQuery = baseQuery.Where(d =>
                d.EventTime >= requestedStartDate &&
                d.EventTime < requestedEndDateExclusive);

            // --- Dedup (unique) still happens once, across ALL categories, exactly like before ---
            // Difference: this stays an IQueryable (translated to SQL), it is not materialized here.
            IQueryable<DeviceAuthEvent> workingQuery;

            if (unique)
            {
                var withMac = baseQuery.Where(x => x.MacAddress != null && x.MacAddress != "");
                var withoutMac = baseQuery.Where(x => x.MacAddress == null || x.MacAddress == "");

                var latestPerMac = withMac
                    .GroupBy(x => x.MacAddress)
                    .Select(g => new { MacAddress = g.Key, MaxEventTime = g.Max(x => x.EventTime) });

                var dedupedWithMac = withMac
                    .Join(
                        latestPerMac,
                        x => new { x.MacAddress, x.EventTime },
                        l => new { l.MacAddress, EventTime = l.MaxEventTime },
                        (x, l) => x);

                // Rows without a MAC address have nothing to dedupe against — keep them as-is
                workingQuery = dedupedWithMac.Concat(withoutMac);
            }
            else
            {
                workingQuery = baseQuery;
            }

            // --- Split into three category queries (still IQueryable, no data pulled yet) ---
            IQueryable<DeviceAuthEvent> avQ = workingQuery
                .Where(d => d.Status == "UNHEALTHY" && d.QuarantineCategory == "AV_Issue");

            IQueryable<DeviceAuthEvent> adQ = workingQuery
                .Where(d => (d.Status == "REJECT" || d.Status == "TIMEOUT") && d.QuarantineCategory == "AD_Issue");

            IQueryable<DeviceAuthEvent> otherQ = workingQuery
                .Where(d => d.QuarantineCategory == "Other");

            // --- Search filter, applied in SQL instead of in-memory ---
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();

                Expression<Func<DeviceAuthEvent, bool>> filter = d =>
                    (d.MacAddress != null && d.MacAddress.ToLower().Contains(s)) ||
                    (d.Hostname != null && d.Hostname.ToLower().Contains(s)) ||
                    (d.BranchCode != null && d.BranchCode.ToLower().Contains(s)) ||
                    (d.CircleCode != null && d.CircleCode.ToLower().Contains(s));

                avQ = avQ.Where(filter);
                adQ = adQ.Where(filter);
                otherQ = otherQ.Where(filter);
            }

            // --- Reason filter, applied in SQL as an IN clause ---
            if (avReason != null && avReason.Count > 0)
                avQ = avQ.Where(x => avReason.Contains(x.QuarantineReason));

            if (adReason != null && adReason.Count > 0)
                adQ = adQ.Where(x => adReason.Contains(x.QuarantineReason));

            if (otherReason != null && otherReason.Count > 0)
                otherQ = otherQ.Where(x => otherReason.Contains(x.QuarantineReason));

            // --- Totals: SQL COUNT(), not client-side Count() over materialized lists ---
            int avTotal = await avQ.CountAsync();
            int adTotal = await adQ.CountAsync();
            int otherTotal = await otherQ.CountAsync();

            // --- Metrics: also SQL COUNT() with predicates ---
            int avNotInstalled = await avQ.CountAsync(x => !x.AVInstalled);
            int avNotUpdated = await avQ.CountAsync(x => x.AVInstalled && !x.AVUpdated);
            int avNotScanned = await avQ.CountAsync(x => x.AVInstalled && !x.AVScanned);

            int adDisabled = await adQ.CountAsync(x => x.ADAccountDisabled);
            int adNotFound = await adQ.CountAsync(x => !x.ADUserFound);
            int adCertInvalid = await adQ.CountAsync(x =>
                x.QuarantineReason != null &&
                x.QuarantineReason.Contains("certificate"));

            int otherAuthFail = await otherQ.CountAsync(x =>
                x.QuarantineReason != null &&
                x.QuarantineReason.Contains("Authentication Failed"));

            int otherMisc = otherTotal - otherAuthFail;

            // --- IP lookup: only need distinct MACs from the (already filtered) AV query,
            //     which is a superset-safe subset of what the original code looked up ---
            var avMacAddresses = await avQ
                .Where(x => !string.IsNullOrEmpty(x.MacAddress))
                .Select(x => x.MacAddress)
                .Distinct()
                .ToListAsync();

            var inventoryDevices = await _db.DeviceInventory
                .AsNoTracking()
                .Where(x => avMacAddresses.Contains(x.MacAddress))
                .Select(x => new { x.MacAddress, x.IpAddress })
                .ToListAsync();

            var ipLookup = inventoryDevices
                .GroupBy(x => x.MacAddress)
                .ToDictionary(g => g.Key, g => g.First().IpAddress);

            // --- Pagination: SQL OFFSET/FETCH via Skip/Take, only pageSize rows come back ---
            var avPagedEntities = await avQ
                .OrderByDescending(x => x.EventTime)
                .Skip((avPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var avPaged = avPagedEntities
                .Select(x =>
                {
                    var dto = MapToDeviceDetails(x);

                    if (!string.IsNullOrEmpty(x.MacAddress) &&
                        ipLookup.TryGetValue(x.MacAddress, out var ip))
                    {
                        dto.IpAddress = ip;
                    }

                    return dto;
                })
                .ToList();

            var adPagedEntities = await adQ
                .OrderByDescending(x => x.EventTime)
                .Skip((adPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var adPaged = adPagedEntities.Select(MapToDeviceDetails).ToList();

            var otherPagedEntities = await otherQ
                .OrderByDescending(x => x.EventTime)
                .Skip((otherPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var otherPaged = otherPagedEntities.Select(MapToDeviceDetails).ToList();

            // --- Available reasons: SQL DISTINCT/ORDER BY instead of in-memory ---
            var avReasons = await avQ
                .Where(x => x.QuarantineReason != null && x.QuarantineReason != "")
                .Select(x => x.QuarantineReason)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var adReasons = await adQ
                .Where(x => x.QuarantineReason != null && x.QuarantineReason != "")
                .Select(x => x.QuarantineReason)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var otherReasons = await otherQ
                .Where(x => x.QuarantineReason != null && x.QuarantineReason != "")
                .Select(x => x.QuarantineReason)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var otherReasonCounts = await otherQ
    .Where(x => x.QuarantineReason != null && x.QuarantineReason != "")
    .GroupBy(x => x.QuarantineReason)
    .Select(g => new { Reason = g.Key, Count = g.Count() })
    .ToListAsync();

            return new QuarantineDetailsDto
            {
                AVIssues = new AVQuarantineMetrics
                {
                    NotInstalled = avNotInstalled,
                    NotUpdated = avNotUpdated,
                    NotScanned = avNotScanned,
                    Devices = new PagedResult<DeviceDetailsDto>
                    {
                        Items = avPaged,
                        TotalCount = avTotal,
                        Page = avPage,
                        PageSize = pageSize,
                        TotalPages = (int)Math.Ceiling((double)avTotal / pageSize)
                    },
                    AvailableReasons = avReasons
                },

                ADIssues = new ADQuarantineMetrics
                {
                    AccountDisabled = adDisabled,
                    UserNotFound = adNotFound,
                    CertificateInvalid = adCertInvalid,
                    Devices = new PagedResult<DeviceDetailsDto>
                    {
                        Items = adPaged,
                        TotalCount = adTotal,
                        Page = adPage,
                        PageSize = pageSize,
                        TotalPages = (int)Math.Ceiling((double)adTotal / pageSize)
                    },
                    AvailableReasons = adReasons
                },

                OtherIssues = new OtherQuarantineMetrics
                {
                    AuthenticationFailed = otherAuthFail,
                    Other = otherMisc,
                    Devices = new PagedResult<DeviceDetailsDto>
                    {
                        Items = otherPaged,
                        TotalCount = otherTotal,
                        Page = otherPage,
                        PageSize = pageSize,
                        TotalPages = (int)Math.Ceiling((double)otherTotal / pageSize)
                    },
                    AvailableReasons = otherReasons,
                     ReasonCounts = otherReasonCounts.ToDictionary(x => x.Reason, x => x.Count)
                }
            };
        }

        // ── Reusable helpers ──────────────────────────────────────────────────────────
        private async Task<List<DeviceDetailsDto>> GetPagedDevicesAsync(
            IQueryable<DeviceAuthEvent> query, int page, int pageSize)
        {
            return await query
                .OrderByDescending(d => d.EventTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DeviceDetailsDto
                {
                    MacAddress = d.MacAddress,
                    Hostname = d.Hostname,
                    Username = d.Username,
                    NasIp = d.NasIp,
                    AuthType = d.AuthType,
                    AuthMethod = d.AuthMethod,
                    HealthStatus = d.HealthStatus,
                    Status = d.Status,
                    CircleCode = d.CircleCode,
                    BranchCode = d.BranchCode,
                    EventTime = d.EventTime,
                    QuarantineCategory = d.QuarantineCategory,
                    QuarantineReason = d.QuarantineReason,
                    IsolationState = d.IsolationState,
                    AVInstalled = d.AVInstalled,
                    AVUpdated = d.AVUpdated,
                    AVScanned = d.AVScanned,
                    AVProduct = d.AVProduct,
                    ADUserFound = d.ADUserFound,
                    ADAccountDisabled = d.ADAccountDisabled,
                    ADDomain = d.ADDomain,
                })
                .ToListAsync();
        }

        private async Task<List<string>> GetAvailableReasonsAsync(IQueryable<DeviceAuthEvent> query)
        {
            return await query
                .Where(d => d.QuarantineReason != null && d.QuarantineReason != "")
                .Select(d => d.QuarantineReason)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();
        }

        public async Task<List<DeviceDetailsDto>> GetQuarantineDevicesForExport(
    string tenantId,
    string category,
    string circleCode = null,
    string search = null,
    List<string> avReason = null,
    List<string> adReason = null,
    List<string> otherReason = null,
    DateTime? startDate = null,
    DateTime? endDate = null,
    bool unique = false)
        {
            var query = _db.DeviceAuthEvents
                .AsNoTracking()
                .Where(d =>
                    (d.Status == "UNHEALTHY" && d.QuarantineCategory == "AV_Issue")
                    ||
                    ((d.Status == "REJECT" || d.Status == "TIMEOUT") && d.QuarantineCategory == "AD_Issue")
                    ||
                    d.QuarantineCategory == "Other"
                );

            if (!string.IsNullOrEmpty(tenantId))
                query = query.Where(d => d.TenantId == tenantId);

            if (!string.IsNullOrEmpty(circleCode))
                query = query.Where(d => d.CircleCode == circleCode);

            var today = DateTime.Today;
            var minimumAllowedDate = today.AddDays(-6);


            var requestedStartDate = startDate?.Date ?? today;
            var requestedEndDate = endDate?.Date ?? today;

            if (requestedStartDate > requestedEndDate)
                throw new ArgumentException("Start date cannot be greater than end date.");

            if (requestedStartDate < minimumAllowedDate)
                throw new ArgumentException("Custom date range can only include the last 7 days.");

            if (requestedEndDate > today)
                throw new ArgumentException("End date cannot be in the future.");

            var requestedEndDateExclusive = requestedEndDate.AddDays(1);

            query = query.Where(d =>
                d.EventTime >= requestedStartDate &&
                d.EventTime < requestedEndDateExclusive);

            List<DeviceAuthEvent> devices;

            if (unique)
            {
                devices = await query
                    .Where(x => !string.IsNullOrEmpty(x.MacAddress))
                    .GroupBy(x => x.MacAddress)
                    .Select(g => g.OrderByDescending(x => x.EventTime).First())
                    .ToListAsync();
            }
            else
            {
                devices = await query
                    .OrderByDescending(x => x.EventTime)
                    .ToListAsync();
            }

            IEnumerable<DeviceAuthEvent> categoryDevices = category switch
            {
                "AV" => devices.Where(d => d.Status == "UNHEALTHY" && d.QuarantineCategory == "AV_Issue"),
                "AD" => devices.Where(d => (d.Status == "REJECT" || d.Status == "TIMEOUT") && d.QuarantineCategory == "AD_Issue"),
                "Other" => devices.Where(d => d.QuarantineCategory == "Other"),
                _ => throw new ArgumentException($"Invalid category: {category}")
            };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                categoryDevices = categoryDevices.Where(d =>
                    (!string.IsNullOrEmpty(d.MacAddress) && d.MacAddress.ToLower().Contains(s)) ||
                    (!string.IsNullOrEmpty(d.Hostname) && d.Hostname.ToLower().Contains(s)) ||
                    (!string.IsNullOrEmpty(d.BranchCode) && d.BranchCode.ToLower().Contains(s)) ||
                    (!string.IsNullOrEmpty(d.CircleCode) && d.CircleCode.ToLower().Contains(s)));
            }

            var reasonFilter = category switch
            {
                "AV" => avReason,
                "AD" => adReason,
                "Other" => otherReason,
                _ => null
            };
            if (reasonFilter != null && reasonFilter.Count > 0)
                categoryDevices = categoryDevices.Where(x => reasonFilter.Contains(x.QuarantineReason));

            var ordered = categoryDevices
                .OrderByDescending(x => x.EventTime)
                .Take(50000)
                .ToList();

            if (category == "AV")
            {
                var macs = ordered
                    .Where(x => !string.IsNullOrEmpty(x.MacAddress))
                    .Select(x => x.MacAddress)
                    .Distinct()
                    .ToList();

                var inventory = await _db.DeviceInventory
                    .AsNoTracking()
                    .Where(x => macs.Contains(x.MacAddress))
                    .Select(x => new { x.MacAddress, x.IpAddress })
                    .ToListAsync();

                var ipLookup = inventory
                    .GroupBy(x => x.MacAddress)
                    .ToDictionary(g => g.Key, g => g.First().IpAddress);

                return ordered.Select(x =>
                {
                    var dto = MapToDeviceDetails(x);
                    if (!string.IsNullOrEmpty(x.MacAddress) && ipLookup.TryGetValue(x.MacAddress, out var ip))
                        dto.IpAddress = ip;
                    return dto;
                }).ToList();
            }

            return ordered.Select(MapToDeviceDetails).ToList();
        }

        public async Task<List<NASDeviceDto>> GetNasDevicesForExport(
     string? tenantId,
     string? circleCode = null,
     bool? activeOnly = null,       // null = All, true = Active, false = Inactive
     string? search = null,
     DateTime? startDate = null,
     DateTime? endDate = null)
        {
            var query = _db.NasDevices.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                query = query.Where(d => d.TenantId == tenantId);

            if (!string.IsNullOrEmpty(circleCode) && !circleCode.Equals("All", StringComparison.OrdinalIgnoreCase))
                query = query.Where(d => d.CircleCode == circleCode);

            if (activeOnly.HasValue)
                query = query.Where(d => d.IsActive == activeOnly.Value);

            // Same default as the grid: no date selected = today
            var today = DateTime.Today;
            var start = startDate?.Date ?? today;
            var end = endDate?.Date ?? today;

            if (start > end)
                throw new ArgumentException("Start date cannot be greater than end date.");

            var endExclusive = end.AddDays(1);
            query = query.Where(d => d.LastSeen >= start && d.LastSeen < endExclusive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(d =>
                    (d.NasIp != null && d.NasIp.ToLower().Contains(s)) ||
                    (d.CircleCode != null && d.CircleCode.ToLower().Contains(s)) ||
                    (d.SourceSystem != null && d.SourceSystem.ToLower().Contains(s)));
            }

            return await query
                .OrderByDescending(d => d.LastSeen)
                .Take(50000)
                .Select(d => new NASDeviceDto
                {
                    NasIp = d.NasIp,
                    SourceSystem = d.SourceSystem,
                    EventType = d.EventType,
                    Circle = d.CircleCode ?? "Not Known",
                    IsActive = d.IsActive,
                    Status = d.IsActive ? "Active" : "Inactive",
                    LastSeen = d.LastSeen
                })
                .ToListAsync();
        }

        /// <summary>
        /// g) Gets comparative metrics for last N days
        /// </summary>
        public async Task<ComparativeMetricsDto> GetComparativeMetricsAsync(int days, string tenantId, string circleCode = null)
        {
            if (days <= 0) days = 7;
            if (days > 90) days = 90; // Cap at 90 days

            var startDate = DateTime.UtcNow.Date.AddDays(-days);
            var dailyMetrics = new List<HistoricalMetricsDto>();

            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i);
                var nextDate = date.AddDays(1);

                var query = _db.DeviceAuthEvents
                    .Where(d => d.EventTime >= date && d.EventTime < nextDate);

                if (!string.IsNullOrEmpty(tenantId))
                    query = query.Where(d => d.TenantId == tenantId);

                if (!string.IsNullOrEmpty(circleCode))
                    query = query.Where(d => d.CircleCode == circleCode);

                var devices = await query
                    .GroupBy(d => d.MacAddress)
                    .Select(g => g.OrderByDescending(d => d.EventTime).FirstOrDefault())
                    .ToListAsync();

                var total802_1x = devices.Count(d => d.AuthType == "802.1x" &&
                                                      d.Username != null &&
                                                      d.Username.StartsWith("host/"));
                var totalMAB = devices.Count(d => d.AuthType == "MAB");
                var healthy = devices.Count(d => d.HealthStatus == "Healthy");
                var unhealthy = devices.Count(d => d.HealthStatus == "Unhealthy");
                var quarantined = devices.Count(d => d.Status == "Quarantine");
                var isolated = devices.Count(d => d.IsolationState == "Isolated");
                var total = devices.Count;

                dailyMetrics.Add(new HistoricalMetricsDto
                {
                    Date = date,
                    Total802_1xMachines = total802_1x,
                    TotalMABMachines = totalMAB,
                    HealthyMachines = healthy,
                    UnhealthyMachines = unhealthy,
                    QuarantinedDevices = quarantined,
                    IsolatedDevices = isolated,
                    HealthPercentage = total > 0 ? (healthy * 100.0 / total) : 0
                });
            }

            // Calculate trends
            var firstDay = dailyMetrics.FirstOrDefault();
            var lastDay = dailyMetrics.LastOrDefault();

            var trends = new TrendSummary
            {
                HealthyDeviceTrend = lastDay != null && firstDay != null
                    ? lastDay.HealthyMachines - firstDay.HealthyMachines
                    : 0,
                UnhealthyDeviceTrend = lastDay != null && firstDay != null
                    ? lastDay.UnhealthyMachines - firstDay.UnhealthyMachines
                    : 0,
                HealthPercentageChange = lastDay != null && firstDay != null
                    ? lastDay.HealthPercentage - firstDay.HealthPercentage
                    : 0
            };

            trends.Outlook = trends.HealthPercentageChange > 2 ? "Improving" :
                            trends.HealthPercentageChange < -2 ? "Degrading" : "Stable";

            return new ComparativeMetricsDto
            {
                DailyMetrics = dailyMetrics,
                Trends = trends
            };
        }

        #region Private Helper Methods

        private async Task<NASMetrics> GetNASMetricsAsync(string tenantId, string circleCode , string timeFilter, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _db.NasDevices.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                query = query.Where(n => n.TenantId == tenantId);


            if (!string.IsNullOrEmpty(circleCode))
                query = query.Where(n => n.CircleCode == circleCode);

            var today = DateTime.Today;
            var minimumAllowedDate = today.AddDays(-6);

            if (!string.Equals(timeFilter, "custom", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Only custom date filter is supported.");
            }
            if (!startDate.HasValue || !endDate.HasValue)
            {
                throw new ArgumentException(
                    "Start date and end date are required.");
            }

            var customStartDate = startDate.Value.Date;
            var customEndDate = endDate.Value.Date;

            if (customStartDate > customEndDate)
            {
                throw new ArgumentException(
                    "Start date cannot be greater than end date.");
            }

            if (customStartDate < minimumAllowedDate)
            {
                throw new ArgumentException(
                    "Date range can only include data from the last 7 days.");
            }

            if (customEndDate > today)
            {
                throw new ArgumentException(
                    "End date cannot be in the future.");
            }

            var exclusiveEndDate = customEndDate.AddDays(1);
            query = query.Where(n =>
       n.LastSeen >= customStartDate &&
       n.LastSeen < exclusiveEndDate);

            var nasDevices = await query.ToListAsync();

            var totalNAS = nasDevices.Count;
            var activeNAS = nasDevices.Count(n => n.IsActive && n.RequestCount > 0);
            var inactiveNAS = totalNAS - activeNAS;

            return new NASMetrics
            {
                TotalNASDevices = totalNAS,
                ActiveNASDevices = activeNAS,
                InactiveNASDevices = inactiveNAS,
                Details = nasDevices.Select(n => new NASDeviceDto
                {
                    NasIp = n.NasIp,
                    //BranchCode = n.BranchCode,
                    //BranchName = n.BranchName,
                    SourceSystem = n.SourceSystem,
                    EventType = n.EventType,
                    RequestCount = n.RequestCount,
                    LastSeen = n.LastSeen,
                    IsActive = n.IsActive,
                    Status = n.Status
                }).ToList()
            };
        }

        private async Task<ClusterMetrics> GetClusterMetricsAsync(string tenantId, string timeFilter)
        {
            var query = _db.ClusterNodes.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                query = query.Where(c => c.TenantId == tenantId);

            if (!string.IsNullOrEmpty(timeFilter))
            {
                var today = DateTime.Today;

                switch (timeFilter.ToLower())
                {
                    case "today":
                        query = query.Where(c => c.EventTime >= today);
                        break;

                    case "week":
                        query = query.Where(c => c.EventTime >= today.AddDays(-7));
                        break;
                }
            }

            // Get latest status per node
            var nodes = await query
                .GroupBy(c => c.NodeIp)
                .Select(g => g.OrderByDescending(c => c.EventTime).FirstOrDefault())
                .ToListAsync();

            var totalNodes = nodes.Count;
            var healthyNodes = nodes.Count(n => n.Status == "Healthy");
            var degradedNodes = totalNodes - healthyNodes;

            return new ClusterMetrics
            {
                TotalNodes = totalNodes,
                HealthyNodes = healthyNodes,
                DegradedNodes = degradedNodes,
                OverallStatus = healthyNodes == totalNodes ? "Healthy" :
                               healthyNodes > totalNodes / 2 ? "Partial" : "Degraded",
                Nodes = nodes.Select(n => new ClusterNodeDto
                {
                    NodeIp = n.NodeIp,
                    NodeName = n.NodeName,
                    NodeRole = n.NodeRole,
                    SyncStatus = n.SyncStatus,
                    ReplicationStatus = n.ReplicationStatus,
                    Uptime = n.Uptime,
                    CpuUsage = (float)n.CpuUsage,
                    MemoryUsage = (float)n.MemoryUsage,
                    EventTime = n.EventTime
                }).ToList()
            };
        }

        private async Task<ADIntegrationMetrics> GetADIntegrationMetricsAsync(string tenantId, string circleCode, string timeFilter)
        {
            var query = _db.AdIntegrations
                .Where(a => a.EventTime >= DateTime.UtcNow.AddHours(-24)); // Last 24 hours

            if (!string.IsNullOrEmpty(tenantId))
                query = query.Where(a => a.TenantId == tenantId);

            if (!string.IsNullOrEmpty(circleCode))
                query = query.Where(a => a.CircleCode == circleCode);

            if (!string.IsNullOrEmpty(timeFilter))
            {
                switch (timeFilter.ToLower())
                {
                    case "today":
                        query = query.Where(a => a.EventTime >= DateTime.Today);
                        break;

                    case "week":
                        query = query.Where(a => a.EventTime >= DateTime.Today.AddDays(-7));
                        break;
                }
            }

            var events = await query.ToListAsync();

            var totalEvents = events.Count;
            var successfulEvents = events.Count(e => e.Status == "Success");
            var failedEvents = totalEvents - successfulEvents;

            var recentFailures = events
                .Where(e => e.Status == "Failed")
                .OrderByDescending(e => e.EventTime)
                .Take(10)
                .Select(e => new ADIntegrationDto
                {
                    AdServer = e.AdServer,
                    DomainName = e.DomainName,
                    FailureReason = e.FailureReason,
                    ErrorCode = e.ErrorCode,
                    Status = e.Status,
                    EventTime = e.EventTime,
                    CircleCode = e.CircleCode
                })
                .ToList();

            return new ADIntegrationMetrics
            {
                TotalADEvents = totalEvents,
                SuccessfulEvents = successfulEvents,
                FailedEvents = failedEvents,
                SuccessRate = totalEvents > 0 ? (successfulEvents * 100.0 / totalEvents) : 100,
                RecentFailures = recentFailures
            };
        }

        //private async Task<List<CircleMetricsDto>> GetCircleBreakdownAsync(string tenantId, string timeFilter, DateTime? startDate = null, DateTime? endDate = null)
        //{
        //    var query = _db.DeviceAuthEvents.AsQueryable();

        //    if (!string.IsNullOrEmpty(tenantId))
        //        query = query.Where(d => d.TenantId == tenantId);

        //    // Date filter
        //    if (!string.Equals(timeFilter, "custom",
        //        StringComparison.OrdinalIgnoreCase))
        //    {
        //        timeFilter = "custom";
        //    }

        //    // If no date is selected, use today
        //    var today = DateTime.Today;

        //    var customStartDate = startDate?.Date ?? today;
        //    var customEndDate = endDate?.Date ?? today;

        //    var minimumAllowedDate = today.AddDays(-6);

        //    if (customStartDate > customEndDate)
        //    {
        //        throw new ArgumentException(
        //            "Start date cannot be greater than end date.");
        //    }

        //    if (customStartDate < minimumAllowedDate)
        //    {
        //        throw new ArgumentException(
        //            "Date range can only include the last 7 days.");
        //    }

        //    if (customEndDate > today)
        //    {
        //        throw new ArgumentException(
        //            "End date cannot be in the future.");
        //    }

        //    var exclusiveEndDate = customEndDate.AddDays(1);

        //    query = query.Where(d =>
        //        d.EventTime >= customStartDate &&
        //        d.EventTime < exclusiveEndDate);

        //    // Get latest event per device per circle
        //    var circleGroups = await query
        //        .GroupBy(d => new { d.MacAddress, d.CircleCode })
        //        .Select(g => g.OrderByDescending(d => d.EventTime).FirstOrDefault())
        //        .ToListAsync();

        //    var circleBreakdown = circleGroups
        //        .GroupBy(d => d.CircleCode)
        //        .Select(g => new CircleMetricsDto
        //        {
        //            CircleCode = g.Key,
        //            CircleName = g.Key, // Would lookup from CircleMaster table
        //            Total802_1xMachines = g.Count(d => d.AuthType == "802.1x" &&
        //                                                d.Username != null &&
        //                                                d.Username.StartsWith("host/")),
        //            TotalMABMachines = g.Count(d => d.AuthType == "MAB"),
        //            TotalHealthyMachines = g.Count(d => d.HealthStatus == "Healthy"),
        //            TotalUnhealthyMachines = g.Count(d => d.HealthStatus == "Unhealthy"),

        //            TotalDevices = g.Count(),
        //            HealthPercentage = g.Count() > 0 ? (g.Count(d => d.HealthStatus == "Healthy") * 100.0 / g.Count()) : 0
        //        })
        //        .OrderBy(c => c.CircleCode)
        //        .ToList();

        //    return circleBreakdown;
        //}

        private async Task<List<CircleMetricsDto>> GetCircleBreakdownAsync(
    string tenantId,
    string timeFilter,
    DateTime? startDate = null,
    DateTime? endDate = null)
        {
            var today = DateTime.Today;

            var customStartDate = startDate?.Date ?? today;
            var customEndDate = endDate?.Date ?? today;

            var minimumAllowedDate = today.AddDays(-6);

            if (customStartDate > customEndDate)
            {
                throw new ArgumentException(
                    "Start date cannot be greater than end date.");
            }

            if (customStartDate < minimumAllowedDate)
            {
                throw new ArgumentException(
                    "Date range can only include the last 7 days.");
            }

            if (customEndDate > today)
            {
                throw new ArgumentException(
                    "End date cannot be in the future.");
            }

            var exclusiveEndDate = customEndDate.AddDays(1);

            // 1. Filter only required tenant/date range
            var filteredQuery = _db.DeviceAuthEvents
                .AsNoTracking()
                .Where(d =>
                    d.TenantId == tenantId &&
                    d.EventTime >= customStartDate &&
                    d.EventTime < exclusiveEndDate);

            // 2. Get latest EventTime for each MAC + Circle
            var latestEvents = filteredQuery
                .GroupBy(d => new
                {
                    d.MacAddress,
                    d.CircleCode
                })
                .Select(g => new
                {
                    g.Key.MacAddress,
                    g.Key.CircleCode,
                    EventTime = g.Max(x => x.EventTime)
                });

            // 3. Join back to get the actual latest event
            var circleGroups = await (
                from d in filteredQuery
                join latest in latestEvents
                    on new
                    {
                        d.MacAddress,
                        d.CircleCode,
                        d.EventTime
                    }
                    equals new
                    {
                        latest.MacAddress,
                        latest.CircleCode,
                        latest.EventTime
                    }
                select new
                {
                    d.MacAddress,
                    d.CircleCode,
                    d.AuthType,
                    d.Username,
                    d.HealthStatus
                }
            ).ToListAsync();

            // 4. Build circle metrics in memory
            var circleBreakdown = circleGroups
                .GroupBy(d => d.CircleCode)
                .Select(g =>
                {
                    var totalDevices = g.Count();

                    var healthyDevices = g.Count(d =>
                        d.HealthStatus == "Healthy");

                    return new CircleMetricsDto
                    {
                        CircleCode = g.Key,
                        CircleName = g.Key,

                        Total802_1xMachines = g.Count(d =>
                            d.AuthType == "802.1x" &&
                            d.Username != null &&
                            d.Username.StartsWith("host/")),

                        TotalMABMachines = g.Count(d =>
                            d.AuthType == "MAB"),

                        TotalHealthyMachines = healthyDevices,

                        TotalUnhealthyMachines = g.Count(d =>
                            d.HealthStatus == "Unhealthy"),

                        TotalDevices = totalDevices,

                        HealthPercentage = totalDevices > 0
                            ? healthyDevices * 100.0 / totalDevices
                            : 0
                    };
                })
                .OrderBy(c => c.CircleCode)
                .ToList();

            return circleBreakdown;
        }

        private async Task<string> GetCircleName(string circleCode)
        {
            var circle = await _db.CircleMaster
                .Where(c => c.CircleCode == circleCode)
                .Select(c => c.CircleName)
                .FirstOrDefaultAsync();

            return circle ?? circleCode;
        }

        private DeviceDetailsDto MapToDeviceDetails(Core.Entities.DeviceAuthEvent authEvent)
        {
            return new DeviceDetailsDto
            {
                MacAddress = authEvent.MacAddress,
                Hostname = authEvent.Hostname,
                Username = authEvent.Username,
                NasIp = authEvent.NasIp,
                AuthType = authEvent.AuthType,
                AuthMethod = authEvent.AuthMethod,
                HealthStatus = authEvent.HealthStatus,
                Status = authEvent.Status,
                CircleCode = authEvent.CircleCode,
                BranchCode = authEvent.BranchCode,
                EventTime = authEvent.EventTime,
                QuarantineCategory = authEvent.QuarantineCategory,
                QuarantineReason = authEvent.QuarantineReason,
                IsolationState = authEvent.IsolationState,
                AVInstalled = authEvent.AVInstalled,
                AVUpdated = authEvent.AVUpdated,
                AVScanned = authEvent.AVScanned,
                AVProduct = authEvent.AVProduct,
                ADUserFound = authEvent.ADUserFound,
                ADAccountDisabled = authEvent.ADAccountDisabled,
                ADDomain = authEvent.ADDomain
            };
        }

        #endregion
    }
}