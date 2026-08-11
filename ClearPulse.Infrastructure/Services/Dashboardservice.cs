using Azure;
using ClearPulse.Core.DTOs;
using ClearPulse.Core.Entities;
using ClearPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace ClearPulse.Infrastructure.Services
{
    public interface IDashboardService
    {
        Task<DashboardMetricsDto> GetUnifiedDashboardAsync(string tenantId, string circleCode, string timeFilter);
        Task<CircleMetricsDto> GetCircleMetricsAsync(string circleCode, string tenantId);
        Task<List<DeviceDetailsDto>> GetDeviceDetailsAsync(string tenantId, string circleCode,
            string authType = null, string healthStatus = null, bool machinesOnly = false, int take = 500, string timeFilter = "today");
        //Task<QuarantineDetailsDto> GetQuarantineDetailsAsync(string tenantId, string circleCode = null);
        Task<QuarantineDetailsDto> GetQuarantineDetailsAsync(
               string tenantId,
               string circleCode = null,
               int avPage = 1,
               int adPage = 1,
               int otherPage = 1,
               int pageSize = 10,
               string search = null,
               string avReason = null,
               string adReason = null,
               string otherReason = null,
               string timeFilter = "today");
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
        public async Task<DashboardMetricsDto> GetUnifiedDashboardAsync(string tenantId, string circleCode, string timeFilter)
        {
            // Base query for device auth events
            var baseQuery = _db.DeviceAuthEvents.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                baseQuery = baseQuery.Where(d => d.TenantId == tenantId);

            if (!string.IsNullOrEmpty(circleCode))
                baseQuery = baseQuery.Where(d => d.CircleCode == circleCode);

            if (!string.IsNullOrEmpty(timeFilter))
            {
                var today = DateTime.Today;

                switch (timeFilter.ToLower())
                {
                    case "today":
                        baseQuery = baseQuery.Where(x => x.EventTime >= today);
                        break;

                    case "week":
                        baseQuery = baseQuery.Where(x => x.EventTime >= today.AddDays(-7));
                        break;
                }
            }
            // Get latest event per device (by MAC address)
            var latestDevices = await baseQuery
                .AsNoTracking()
                .GroupBy(d => d.MacAddress)
                .Select(g => g.OrderByDescending(d => d.EventTime).FirstOrDefault())
                .ToListAsync();

            // a) Total 802.1x machines
            var total802_1x = latestDevices.Count(d => d.AuthType == "802.1x" &&
                                            d.HealthStatus == "Healthy");

            // b) Total MAB machines
            var totalMAB = latestDevices.Count(d => d.AuthType == "MAB" &&
                                         d.HealthStatus == "Healthy");

            // c) Total healthy machines
            var totalHealthy = latestDevices.Count(d => d.HealthStatus == "Healthy");

            // d) Total unhealthy machines
            var totalUnhealthy = latestDevices.Count(d => d.HealthStatus == "Unhealthy");

            // e) NAS metrics
            var nasMetrics = await GetNASMetricsAsync(tenantId, circleCode , timeFilter);

            // h) Cluster metrics
            //var clusterMetrics = await GetClusterMetricsAsync(tenantId , timeFilter);

            // i) AD integration metrics
            //var adMetrics = await GetADIntegrationMetricsAsync(tenantId, circleCode , timeFilter);

            // AD / AV 
            var quarantine = await GetQuarantineDetailsAsync(tenantId,circleCode, 1,1,1, 10,null,null,null,null,timeFilter);

            var devicesMetrics = await GetDeviceDetailsAsync(tenantId, circleCode, null, null, false, 500, timeFilter);

            // f) Circle-wise breakdown
            var circleBreakdown = await GetCircleBreakdownAsync(tenantId , timeFilter);

            return new DashboardMetricsDto
            {
                LastUpdated = DateTime.UtcNow,
                TenantId = tenantId,
                Total802_1xMachines = total802_1x,
                TotalMABMachines = totalMAB,
                TotalHealthyMachines = totalHealthy,
                TotalUnhealthyMachines = totalUnhealthy,
                NASMetrics = nasMetrics,
                //ClusterMetrics = clusterMetrics,
                deviceDetails = devicesMetrics,
                //ADMetrics = adMetrics,
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
             string timeFilter = "today")
        {
            if (take <= 0) take = 100;
            if (take > 2000) take = 2000;

            // Use DeviceInventory for current state
            var query = _db.DeviceInventory.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                query = query.Where(d => d.TenantId == tenantId);

            if (!string.IsNullOrEmpty(circleCode))
                query = query.Where(d => d.CircleCode == circleCode);

            if (!string.IsNullOrEmpty(timeFilter))
            {
                switch (timeFilter.ToLower())
                {
                    case "today":
                        query = query.Where(d => d.LastSeenOnline >= DateTime.Today);
                        break;

                    case "week":
                        query = query.Where(d => d.LastSeenOnline >= DateTime.Today.AddDays(-7));
                        break;
                }
            }

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
            var latestAuthEvents = await _db.DeviceAuthEvents
                .Where(e => macAddresses.Contains(e.MacAddress))
                .GroupBy(e => e.MacAddress)
                .Select(g => g.OrderByDescending(e => e.EventTime).FirstOrDefault())
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

        //    public async Task<QuarantineDetailsDto> GetQuarantineDetailsAsync(
        //string tenantId,
        //string circleCode = null,
        //int avPage = 1,
        //int adPage = 1,
        //int otherPage = 1,
        //int pageSize = 10,
        //string search = null,
        //string avReason = null,
        //string adReason = null,
        //string otherReason = null,
        //string timeFilter = "today")
        //    {
        //        // ── 1. Base filter ────────────────────────────────────────────────────
        //        var baseQuery = _db.DeviceAuthEvents
        //            .AsNoTracking()
        //            .Where(d => d.Status == "Quarantine" || d.IsolationState == "Isolated");

        //        if (!string.IsNullOrEmpty(tenantId))
        //            baseQuery = baseQuery.Where(d => d.TenantId == tenantId);
        //        if (!string.IsNullOrEmpty(circleCode))
        //            baseQuery = baseQuery.Where(d => d.CircleCode == circleCode);

        //        if (!string.IsNullOrEmpty(timeFilter))
        //        {
        //            switch (timeFilter.ToLower())
        //            {
        //                case "today":
        //                    baseQuery = baseQuery.Where(a => a.EventTime >= DateTime.Today);
        //                    break;

        //                case "week":
        //                    baseQuery = baseQuery.Where(a => a.EventTime >= DateTime.Today.AddDays(-7));
        //                    break;
        //            }
        //        }

        //        // ── 2. Latest per device ──────────────────────────────────────────────
        //        var latestTimes = baseQuery
        //            .GroupBy(d => d.MacAddress)
        //            .Select(g => new { MacAddress = g.Key, MaxEventTime = g.Max(d => d.EventTime) });

        //        var latestDevices =
        //            from d in baseQuery
        //            join lt in latestTimes
        //                on new { d.MacAddress, d.EventTime }
        //                equals new { lt.MacAddress, EventTime = lt.MaxEventTime }
        //            select d;

        //        // ── 3. Category split ─────────────────────────────────────────────────
        //        var avQuery = latestDevices.Where(d => d.QuarantineCategory == "AV_Issue");
        //        var adQuery = latestDevices.Where(d => d.QuarantineCategory == "AD_Issue");
        //        var otherQuery = latestDevices.Where(d =>
        //            d.QuarantineCategory == "Other" ||
        //            d.QuarantineCategory == null ||
        //            d.QuarantineCategory == "");

        //        // ── 4. Apply search to all categories ────────────────────────────────
        //        if (!string.IsNullOrEmpty(search))
        //        {
        //            search = search.Trim();
        //            avQuery = avQuery.Where(d => d.MacAddress.Contains(search) || d.Hostname.Contains(search) || d.BranchCode.Contains(search) || d.CircleCode.Contains(search));
        //            adQuery = adQuery.Where(d => d.MacAddress.Contains(search) || d.Hostname.Contains(search) || d.BranchCode.Contains(search) || d.CircleCode.Contains(search));
        //            otherQuery = otherQuery.Where(d => d.MacAddress.Contains(search) || d.Hostname.Contains(search) || d.BranchCode.Contains(search) || d.CircleCode.Contains(search));
        //        }

        //        // ── 5. Apply per-tab reason filter ────────────────────────────────────
        //        if (!string.IsNullOrEmpty(avReason))
        //            avQuery = avQuery.Where(d => d.QuarantineReason == avReason);
        //        if (!string.IsNullOrEmpty(adReason))
        //            adQuery = adQuery.Where(d => d.QuarantineReason == adReason);
        //        if (!string.IsNullOrEmpty(otherReason))
        //            otherQuery = otherQuery.Where(d => d.QuarantineReason == otherReason);

        //        // ── 6. Summary metrics ────────────────────────────────────────────────
        //        var avNotInstalled = await avQuery.CountAsync(d => !d.AVInstalled);
        //        var avNotUpdated = await avQuery.CountAsync(d => d.AVInstalled && !d.AVUpdated);
        //        var avNotScanned = await avQuery.CountAsync(d => d.AVInstalled && !d.AVScanned);

        //        var adDisabled = await adQuery.CountAsync(d => d.ADAccountDisabled);
        //        var adNotFound = await adQuery.CountAsync(d => !d.ADUserFound);
        //        var adCertInvalid = await adQuery.CountAsync(d => d.QuarantineReason != null && d.QuarantineReason.Contains("certificate"));

        //        var otherAuthFail = await otherQuery.CountAsync(d => d.QuarantineReason != null && d.QuarantineReason.Contains("Authentication Failed"));
        //        var otherMisc = await otherQuery.CountAsync(d => d.QuarantineReason == null || !d.QuarantineReason.Contains("Authentication Failed"));

        //        // ── 7. Paginate all 3 tabs ────────────────────────────────────────────
        //        var avTotal = await avQuery.CountAsync();
        //        var adTotal = await adQuery.CountAsync();
        //        var otherTotal = await otherQuery.CountAsync();
        //        Console.WriteLine($"AD Total = {adTotal}");
        //        var avDevices = await GetPagedDevicesAsync(avQuery, avPage, pageSize);
        //        var adDevices = await GetPagedDevicesAsync(adQuery, adPage, pageSize);
        //        var otherDevices = await GetPagedDevicesAsync(otherQuery, otherPage, pageSize);
        //        Console.WriteLine($"Returned = {adDevices.Count}");
        //        // ── 8. Available reasons per tab ──────────────────────────────────────
        //        var avReasons = await GetAvailableReasonsAsync(avQuery);
        //        var adReasons = await GetAvailableReasonsAsync(adQuery);
        //        var otherReasons = await GetAvailableReasonsAsync(otherQuery);

        //        // ── 9. Assemble ───────────────────────────────────────────────────────
        //        return new QuarantineDetailsDto
        //        {
        //            AVIssues = new AVQuarantineMetrics
        //            {
        //                NotInstalled = avNotInstalled,
        //                NotUpdated = avNotUpdated,
        //                NotScanned = avNotScanned,
        //                Devices = new PagedResult<DeviceDetailsDto>
        //                {
        //                    Items = avDevices,
        //                    TotalCount = avTotal,
        //                    Page = avPage,
        //                    PageSize = pageSize,
        //                    TotalPages = (int)Math.Ceiling((double)avTotal / pageSize)
        //                },
        //                AvailableReasons = avReasons
        //            },
        //            ADIssues = new ADQuarantineMetrics
        //            {
        //                AccountDisabled = adDisabled,
        //                UserNotFound = adNotFound,
        //                CertificateInvalid = adCertInvalid,
        //                Devices = new PagedResult<DeviceDetailsDto>
        //                {
        //                    Items = adDevices,
        //                    TotalCount = adTotal,
        //                    Page = adPage,
        //                    PageSize = pageSize,
        //                    TotalPages = (int)Math.Ceiling((double)adTotal / pageSize)
        //                },
        //                AvailableReasons = adReasons
        //            },
        //            OtherIssues = new OtherQuarantineMetrics
        //            {
        //                AuthenticationFailed = otherAuthFail,
        //                Other = otherMisc,
        //                Devices = new PagedResult<DeviceDetailsDto>
        //                {
        //                    Items = otherDevices,
        //                    TotalCount = otherTotal,
        //                    Page = otherPage,
        //                    PageSize = pageSize,
        //                    TotalPages = (int)Math.Ceiling((double)otherTotal / pageSize)
        //                },
        //                AvailableReasons = otherReasons
        //            }
        //        };
        //    }

        public async Task<QuarantineDetailsDto> GetQuarantineDetailsAsync(
    string tenantId,
    string circleCode = null,
    int avPage = 1,
    int adPage = 1,
    int otherPage = 1,
    int pageSize = 10,
    string search = null,
    string avReason = null,
    string adReason = null,
    string otherReason = null,
    string timeFilter = "today")
        {
            var query = _db.DeviceAuthEvents
                .AsNoTracking()
                .Where(d => d.Status == "Quarantine" || d.IsolationState == "Isolated");

            if (!string.IsNullOrEmpty(tenantId))
                query = query.Where(d => d.TenantId == tenantId);

            if (!string.IsNullOrEmpty(circleCode))
                query = query.Where(d => d.CircleCode == circleCode);

            switch (timeFilter?.ToLower())
            {
                case "today":
                    query = query.Where(d => d.EventTime >= DateTime.Today);
                    break;

                case "week":
                    query = query.Where(d => d.EventTime >= DateTime.Today.AddDays(-7));
                    break;
            }

            // Latest event per MAC
            var latestDevices = await query
                .GroupBy(d => d.MacAddress)
                .Select(g => g.OrderByDescending(x => x.EventTime).First())
                .ToListAsync();

            IEnumerable<DeviceAuthEvent> avDevices = latestDevices
                .Where(d => d.QuarantineCategory == "AV_Issue");

            IEnumerable<DeviceAuthEvent> adDevices = latestDevices
                .Where(d => d.QuarantineCategory == "AD_Issue");

            IEnumerable<DeviceAuthEvent> otherDevices = latestDevices
                .Where(d =>
                    d.QuarantineCategory == "Other" ||
                    string.IsNullOrEmpty(d.QuarantineCategory));

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();

                Func<DeviceAuthEvent, bool> filter = d =>
                    (!string.IsNullOrEmpty(d.MacAddress) && d.MacAddress.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(d.Hostname) && d.Hostname.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(d.BranchCode) && d.BranchCode.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(d.CircleCode) && d.CircleCode.ToLower().Contains(search));

                avDevices = avDevices.Where(filter);
                adDevices = adDevices.Where(filter);
                otherDevices = otherDevices.Where(filter);
            }

            // Reason filter
            if (!string.IsNullOrWhiteSpace(avReason))
                avDevices = avDevices.Where(x => x.QuarantineReason == avReason);

            if (!string.IsNullOrWhiteSpace(adReason))
                adDevices = adDevices.Where(x => x.QuarantineReason == adReason);

            if (!string.IsNullOrWhiteSpace(otherReason))
                otherDevices = otherDevices.Where(x => x.QuarantineReason == otherReason);

            // Totals
            int avTotal = avDevices.Count();
            int adTotal = adDevices.Count();
            int otherTotal = otherDevices.Count();

            // Metrics
            int avNotInstalled = avDevices.Count(x => !x.AVInstalled);
            int avNotUpdated = avDevices.Count(x => x.AVInstalled && !x.AVUpdated);
            int avNotScanned = avDevices.Count(x => x.AVInstalled && !x.AVScanned);

            int adDisabled = adDevices.Count(x => x.ADAccountDisabled);
            int adNotFound = adDevices.Count(x => !x.ADUserFound);
            int adCertInvalid = adDevices.Count(x =>
                !string.IsNullOrEmpty(x.QuarantineReason) &&
                x.QuarantineReason.Contains("certificate", StringComparison.OrdinalIgnoreCase));

            int otherAuthFail = otherDevices.Count(x =>
                !string.IsNullOrEmpty(x.QuarantineReason) &&
                x.QuarantineReason.Contains("Authentication Failed", StringComparison.OrdinalIgnoreCase));

            int otherMisc = otherDevices.Count() - otherAuthFail;

            // Pagination
            var avPaged = avDevices
                .OrderByDescending(x => x.EventTime)
                .Skip((avPage - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDeviceDetails)
                .ToList();

            var adPaged = adDevices
                .OrderByDescending(x => x.EventTime)
                .Skip((adPage - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDeviceDetails)
                .ToList();

            var otherPaged = otherDevices
                .OrderByDescending(x => x.EventTime)
                .Skip((otherPage - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDeviceDetails)
                .ToList();

            // Reasons
            var avReasons = avDevices
                .Where(x => !string.IsNullOrEmpty(x.QuarantineReason))
                .Select(x => x.QuarantineReason)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var adReasons = adDevices
                .Where(x => !string.IsNullOrEmpty(x.QuarantineReason))
                .Select(x => x.QuarantineReason)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var otherReasons = otherDevices
                .Where(x => !string.IsNullOrEmpty(x.QuarantineReason))
                .Select(x => x.QuarantineReason)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

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
                    AvailableReasons = otherReasons
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

        private async Task<NASMetrics> GetNASMetricsAsync(string tenantId, string circleCode , string timeFilter)
        {
            var query = _db.NasDevices.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                query = query.Where(n => n.TenantId == tenantId);

            if (!string.IsNullOrEmpty(circleCode))
                query = query.Where(n => n.CircleCode == circleCode);

            if (!string.IsNullOrEmpty(timeFilter))
            {
                switch (timeFilter.ToLower())
                {
                    case "today":
                        query = query.Where(n => n.LastSeen >= DateTime.Today);
                        break;

                    case "week":
                        query = query.Where(n => n.LastSeen >= DateTime.Today.AddDays(-7));
                        break;
                }
            }

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
                    BranchCode = n.BranchCode,
                    BranchName = n.BranchName,
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

        private async Task<List<CircleMetricsDto>> GetCircleBreakdownAsync(string tenantId, string timeFilter)
        {
            var query = _db.DeviceAuthEvents.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                query = query.Where(d => d.TenantId == tenantId);

            if (!string.IsNullOrEmpty(timeFilter))
            {
                var today = DateTime.Today;
                
                switch (timeFilter.ToLower())
                {
                    case "today":
                        query = query.Where(d => d.EventTime >= today);
                        break;

                    case "week":
                        query = query.Where(d => d.EventTime >= today.AddDays(-7));
                        break;
                }
            }

            // Get latest event per device per circle
            var circleGroups = await query
                .GroupBy(d => new { d.MacAddress, d.CircleCode })
                .Select(g => g.OrderByDescending(d => d.EventTime).FirstOrDefault())
                .ToListAsync();

            var circleBreakdown = circleGroups
                .GroupBy(d => d.CircleCode)
                .Select(g => new CircleMetricsDto
                {
                    CircleCode = g.Key,
                    CircleName = g.Key, // Would lookup from CircleMaster table
                    Total802_1xMachines = g.Count(d => d.AuthType == "802.1x" &&
                                                        d.Username != null &&
                                                        d.Username.StartsWith("host/")),
                    TotalMABMachines = g.Count(d => d.AuthType == "MAB"),
                    TotalHealthyMachines = g.Count(d => d.HealthStatus == "Healthy"),
                    TotalUnhealthyMachines = g.Count(d => d.HealthStatus == "Unhealthy"),

                    TotalDevices = g.Count(),
                    HealthPercentage = g.Count() > 0 ? (g.Count(d => d.HealthStatus == "Healthy") * 100.0 / g.Count()) : 0
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