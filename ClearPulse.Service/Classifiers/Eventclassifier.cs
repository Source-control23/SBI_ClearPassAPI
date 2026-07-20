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
        private readonly ILogger<EventClassifier> _logger;

        public EventClassifier(Parsers.LogParser parser, string defaultTenantId, ILogger<EventClassifier> logger)
        {
            _parser = parser;
            _defaultTenantId = defaultTenantId;
            _logger = logger;
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
                string user = _parser.GetMappedJsonString(root, "username") ?? "";
                var (hostname, circleCode, domain) = _parser.ParseUsername(user);
                string errorCodeStr = _parser.GetMappedJsonString(root, "errorCode");
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
                string nasIp = _parser.GetMappedJsonString(root, "nasIp");
                string user = _parser.GetMappedJsonString(root, "username") ?? "";
                var (hostname, circleCode, domain) = _parser.ParseUsername(user);

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

            try
            {
                // Log incoming raw log
                _logger.LogInformation("========== Device Authentication Log ==========");
                _logger.LogInformation("Raw Log: {RawLog}", line);

                // Extract mapped values
                string mac = _parser.GetMappedJsonString(root, "macAddress");

                if (string.IsNullOrWhiteSpace(user) && string.IsNullOrWhiteSpace(mac))
                {
                    _logger.LogInformation("Skipping log because Username and MacAddress are both empty.");
                    return false;
                }

                string nasIp = _parser.GetMappedJsonString(root, "nasIp");
                string alerts = _parser.GetMappedJsonString(root, "alerts") ?? "";
                string errorCodeStr = _parser.GetMappedJsonString(root, "errorCode");
                string postureToken = _parser.GetMappedJsonString(root, "healthstatus");
                string service = _parser.GetMappedJsonString(root, "service");
                string enforcement = _parser.GetMappedJsonString(root, "enforcement");
                //AV
                string antivirusInput = _parser.GetMappedJsonString(root, "antivirusInput");
                string antivirusOutput = _parser.GetMappedJsonString(root, "antivirusOutput");
                string antivirusApt = _parser.GetMappedJsonString(root, "antivirusApt");
                bool isAVLog = !string.IsNullOrWhiteSpace(antivirusInput) || !string.IsNullOrWhiteSpace(antivirusOutput);

                _logger.LogInformation(
        "Mapped Values | MacAddress={Mac} | Username={User} | NasIp={NasIp} | Alerts={Alerts} | ErrorCode={ErrorCode} | PostureToken={Posture} | Service={Service} | AVInput={AVInput} | AVOutput={AVOutput} | enforcement={enforcement}",
        mac, user, nasIp, alerts, errorCodeStr, postureToken, service, antivirusInput,  antivirusOutput , enforcement);

                int? errorCode = _parser.ExtractErrorCode(errorCodeStr, alerts);

                var (hostname, circleCode, domain) = _parser.ParseUsername(user);

                bool avInstalled = false;
                bool avUpdated = false;
                bool avScanned = false;

                string avProduct = "";
                string avReason = null;
                string avCategory = null;

                if (isAVLog)
                {
                    (avInstalled,
                     avUpdated,
                     avScanned,
                     avProduct,
                     avReason,
                     avCategory) = _parser.ParseAVStatus(antivirusInput);
                }

                var (quarantineCategory, quarantineReason, isolationState) =
                    _parser.ParseQuarantineInfo(alerts, errorCode);

                string finalCategory = isAVLog ? avCategory : quarantineCategory;
                string finalReason = isAVLog ? avReason : quarantineReason;
                var (adUserFound, adAccountDisabled, adCertValid, adDomain) = _parser.ParseADStatus(alerts, errorCode);

                string authType = _parser.DetermineAuthType(service);
                //string authMethod = _parser.DetermineAuthMethod(alerts);
                string healthStatus = _parser.DetermineHealthStatus(postureToken, antivirusApt, errorCode , enforcement);
                _logger.LogInformation("NasIp={NasIp}, Username={Username}, Hostname={Hostname}, AuthType={AuthType}, Health={Health}", nasIp, user, hostname, authType, healthStatus);
                entity = new DeviceAuthEvent
                {
                    MacAddress = mac?.Replace(":", "") ?? "",
                    Username = user ?? "",
                    Hostname = hostname ?? "",
                    NasIp = nasIp ?? "",
                    AuthType = authType ?? "",
                    //AuthMethod = authMethod ?? "",
                    ErrorCode = errorCode,
                    Reason = !string.IsNullOrWhiteSpace(alerts) ? alerts : (isAVLog ? finalReason : null),
                    QuarantineReason = finalReason ?? "",
                    QuarantineCategory = finalCategory ?? "",
                    IsolationState = isolationState,
                    AVInstalled = avInstalled,
                    AVUpdated = avUpdated,
                    AVScanned = avScanned,
                    AVProduct = avProduct ?? "",
                    ADAccountDisabled = adAccountDisabled,
                    ADUserFound = adUserFound,
                    ADCertificateValid = adCertValid,
                    ADDomain = adDomain ?? domain,
                    HealthStatus = healthStatus,
                    CircleCode = circleCode,
                    EventType = "Authentication",
                    EventTime = eventTime,
                    Status = healthStatus == "Healthy" ? "Accept" : "Quarantine",
                    SourceSystem = "ClearPass",
                    MetadataJson = line,
                    TenantId = _defaultTenantId
                };

                _logger.LogInformation(
                    "Successfully created DeviceAuthEvent. MAC={Mac}, User={User}, Health={Health}, AuthType={AuthType} , Reason={Reason}",
                    entity.MacAddress,
                    entity.Username,
                    entity.HealthStatus,
                    entity.AuthType,
                    entity.Reason);

                if (!string.IsNullOrEmpty(finalCategory))
                {
                    quarantine = new QuarantineDetail
                    {
                        MacAddress = entity.MacAddress,
                        QuarantineType = finalCategory,
                        QuarantineReason = finalReason,
                        DetailedMessage = !string.IsNullOrWhiteSpace(alerts) ? alerts: finalReason,
                        QuarantineStartTime = eventTime,
                        IsActive = true,
                        CircleCode = circleCode,
                        EventType = "Quarantine",
                        EventTime = eventTime,
                        Status = "Active",
                        SourceSystem = "ClearPass",
                        MetadataJson = line,
                        TenantId = _defaultTenantId
                    };

                    _logger.LogInformation(
                        "Quarantine record created. Type={Type}, Reason={Reason}",
                        finalCategory,
                        finalReason);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, @"Error while classifying Device Authentication Event. Raw Log: {RawLog}", line);

                return false;
            }
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