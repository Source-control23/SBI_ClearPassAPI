using System;
using System.Text.Json;
using ClearPulse.Core.Entities;

namespace ClearPulse.Service.Classifiers
{
    /// <summary>
    /// Classifies log events and creates appropriate entity types
    /// </summary>
    public class EventClassifier
    {
        private readonly Parsers.LogParser _parser;
        private readonly string _defaultTenantId;

        public EventClassifier(Parsers.LogParser parser, string defaultTenantId)
        {
            _parser = parser;
            _defaultTenantId = defaultTenantId;
        }

        public bool TryClassifyAdIntegration(JsonElement root, string eventCode, string alerts,
            DateTime eventTime, string line, out AdIntegration entity)
        {
            entity = null;

            // AD Integration events: code 4103, 4102 or alerts containing AD-related messages
            if (eventCode == "4103" || eventCode == "4102" ||
                alerts.Contains("Global Catalog") || alerts.Contains("AD Integration") ||
                alerts.Contains("LDAP bind"))
            {
                string user = _parser.GetJsonString(root, "Common.Username") ?? "";
                var (hostname, circleCode, branchCode, domain) = _parser.ParseUsername(user);
                string errorCodeStr = _parser.GetJsonString(root, "Common.Error-Code");
                int? errorCode = _parser.ExtractErrorCode(errorCodeStr, alerts);

                entity = new AdIntegration
                {
                    AdServer = _parser.ExtractAdServer(alerts),
                    DomainName = domain ?? "AD.XYZ",
                    FailureReason = alerts,
                    ErrorCode = errorCode,
                    Status = alerts.Contains("failed") || alerts.Contains("Error") || alerts.Contains("Warning")
                        ? "Failed" : "Success",
                    CircleCode = circleCode,
                    EventType = "AD-Sync",
                    EventTime = eventTime,
                    SourceSystem = "ClearPass",
                    MetadataJson = line,
                    TenantId = _defaultTenantId
                };
                return true;
            }

            return false;
        }

        public bool TryClassifyClusterNode(JsonElement root, string eventCode, string alerts,
            DateTime eventTime, string line, out ClusterNode entity)
        {
            entity = null;

            // Cluster sync events: codes 4001, 3027, 4002 or alerts containing "Cluster"
            if (eventCode == "4001" || eventCode == "3027" || eventCode == "4002" ||
                alerts.Contains("Cluster"))
            {
                string nasIp = _parser.GetJsonString(root, "Common.NAS-IP-Address");
                string user = _parser.GetJsonString(root, "Common.Username") ?? "";
                var (hostname, circleCode, branchCode, domain) = _parser.ParseUsername(user);

                string nodeIp = _parser.ExtractNodeIp(alerts, nasIp);
                string syncStatus = alerts.Contains("Healthy") || alerts.Contains("completed")
                    ? "Healthy" : "Degraded";

                var (cpuUsage, memoryUsage) = _parser.ParseResourceMetrics(root);
                string nodeName = _parser.ExtractNodeName(root, nodeIp);
                string replicationStatus = _parser.DetermineReplicationStatus(syncStatus, alerts);

                entity = new ClusterNode
                {
                    NodeIp = nodeIp,
                    NodeName = nodeName,
                    Site = circleCode,
                    SyncStatus = syncStatus,
                    ReplicationStatus = replicationStatus,
                    Uptime = _parser.GetJsonString(root, "Uptime") ?? "N/A",
                    NodeRole = _parser.GetJsonString(root, "NodeRole") ?? "Subscriber",
                    CpuUsage = cpuUsage,
                    MemoryUsage = memoryUsage,
                    Status = alerts.Contains("Healthy") ? "Healthy" : "Degraded",
                    CircleCode = circleCode,
                    EventType = "ClusterSync",
                    EventTime = eventTime,
                    SourceSystem = "ClearPass",
                    MetadataJson = line,
                    TenantId = _defaultTenantId
                };
                return true;
            }

            return false;
        }

        public bool TryClassifyDeviceAuth(JsonElement root, string user, DateTime eventTime,
            string line, out DeviceAuthEvent entity, out QuarantineDetail quarantine)
        {
            entity = null;
            quarantine = null;

            // Device authentication events - any event with username or MAC address
            if (!string.IsNullOrEmpty(user) || root.TryGetProperty("Common.Host-MAC-Address", out _))
            {
                string mac = _parser.GetJsonString(root, "Common.Host-MAC-Address");
                string nasIp = _parser.GetJsonString(root, "Common.NAS-IP-Address");
                string alerts = _parser.GetJsonString(root, "Common.Alerts") ?? "";
                string errorCodeStr = _parser.GetJsonString(root, "Common.Error-Code");
                int? errorCode = _parser.ExtractErrorCode(errorCodeStr, alerts);

                var (hostname, circleCode, branchCode, domain) = _parser.ParseUsername(user);
                var (quarantineCategory, quarantineReason, isolationState) = _parser.ParseQuarantineInfo(alerts, errorCode);
                var (avInstalled, avUpdated, avScanned, avProduct) = _parser.ParseAVStatus(alerts);
                var (adUserFound, adAccountDisabled, adCertValid, adDomain) = _parser.ParseADStatus(alerts, errorCode);

                string authType = _parser.DetermineAuthType(user, mac);
                string authMethod = _parser.DetermineAuthMethod(alerts);
                string healthStatus = _parser.DetermineHealthStatus(errorCode, alerts, isolationState);

                entity = new DeviceAuthEvent
                {
                    MacAddress = mac?.Replace(":", "") ?? "",
                    Username = user,
                    Hostname = hostname,
                    NasIp = nasIp,
                    AuthType = authType,
                    AuthMethod = authMethod,
                    ErrorCode = errorCode,
                    Reason = alerts,
                    BranchCode = branchCode,
                    QuarantineReason = quarantineReason,
                    QuarantineCategory = quarantineCategory,
                    IsolationState = isolationState,
                    AVInstalled = avInstalled,
                    AVUpdated = avUpdated,
                    AVScanned = avScanned,
                    AVProduct = avProduct,
                    ADAccountDisabled = adAccountDisabled,
                    ADUserFound = adUserFound,
                    ADCertificateValid = adCertValid,
                    ADDomain = adDomain ?? domain,
                    HealthStatus = healthStatus,
                    CircleCode = circleCode,
                    EventType = "Authentication",
                    EventTime = eventTime,
                    Status = errorCode == 0 ? "Accept" : "Quarantine",
                    SourceSystem = "ClearPass",
                    MetadataJson = line,
                    TenantId = _defaultTenantId,
                    WindowsPatchLevel = null, // Would need to be extracted from posture data
                    CriticalPatchesMissing = null,
                    LastPatchDate = null
                };

                // Create quarantine detail if device is isolated
                if (!string.IsNullOrEmpty(quarantineCategory))
                {
                    quarantine = new QuarantineDetail
                    {
                        MacAddress = mac?.Replace(":", "") ?? "",
                        QuarantineType = quarantineCategory,
                        QuarantineReason = quarantineReason,
                        DetailedMessage = alerts,
                        QuarantineStartTime = eventTime,
                        QuarantineEndTime = null,
                        IsActive = true,
                        ResolvedBy = null,
                        ResolutionNotes = null,
                        CircleCode = circleCode,
                        EventType = "Quarantine",
                        EventTime = eventTime,
                        Status = "Active",
                        SourceSystem = "ClearPass",
                        MetadataJson = line,
                        TenantId = _defaultTenantId
                    };
                }

                return true;
            }

            return false;
        }

        public NasDevice CreateNasDevice(string nasIp, string circleCode, DateTime eventTime, string line)
        {
            if (string.IsNullOrEmpty(nasIp))
                return null;

            return new NasDevice
            {
                NasIp = nasIp,
                RequestCount = 1,
                LastSeen = eventTime,
                CircleCode = circleCode,
                EventType = "NAS-Activity",
                EventTime = eventTime,
                Status = "Active",
                SourceSystem = "ClearPass",
                MetadataJson = line,
                TenantId = _defaultTenantId,
                BranchCode = null, // Would need mapping table
                BranchName = null,
                DeviceModel = null,
                FirmwareVersion = null,
                IsActive = true
            };
        }

        public LogEvent CreateGenericLogEvent(DateTime eventTime, string circleCode, string line)
        {
            return new LogEvent
            {
                EventTime = eventTime,
                CircleCode = circleCode,
                EventType = "Unknown",
                Status = "Logged",
                SourceSystem = "ClearPass",
                MetadataJson = line,
                TenantId = _defaultTenantId
            };
        }
    }
}