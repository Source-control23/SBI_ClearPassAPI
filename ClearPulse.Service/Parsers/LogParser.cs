using ClearPulse.Core.DTOs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ClearPulse.Service.Parsers
{
    /// <summary>
    /// Handles parsing of ClearPass JSON log entries
    /// </summary>
    public class LogParser
    {
        private readonly Dictionary<string, List<string>> _fieldMappings;
        private readonly ILogger<LogParser> _logger;
        private static readonly Dictionary<string, string> _avCheckMessages =
        new(StringComparer.OrdinalIgnoreCase)
        {
        ["LastScanTimeCheck"] = "Not Scanned",
        ["DatFileTimeCheck"] = "Not Updated",
        ["EngineVersionCheck"] = "Not Updated",
        ["ProductVersionCheck"] = "Not Updated",
        ["AppNotInstalled"] = "Not Installed"
        };

        public LogParser(string fieldMappingsPath = "Configuration/FieldMappings.json", ILogger<LogParser> logger = null)
        {
            var fullPath = Path.Combine(AppContext.BaseDirectory, fieldMappingsPath);

            var mapJson = File.ReadAllText(fullPath);

            _fieldMappings =
                JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(mapJson)
                ?? new Dictionary<string, List<string>>();
            _logger = logger;
        }
        public string GetJsonString(JsonElement root, params string[] path)
        {
            JsonElement current = root;
            foreach (var key in path)
            {
                if (!current.TryGetProperty(key, out current))
                    return null;
            }
            return current.ValueKind == JsonValueKind.Null ? null : current.ToString();
        }
        public string GetMappedJsonString(JsonElement root, string key)
        {
            if (!_fieldMappings.TryGetValue(key, out var possibleKeys))
            {
                _logger?.LogWarning("No mapping found for key: {Key}", key);
                return null;
            }

            foreach (var mapping in possibleKeys)
            {
                _logger?.LogInformation("Trying mapping '{Mapping}' for key '{Key}'", mapping, key);

                // Exact property
                if (root.TryGetProperty(mapping, out var exact))
                {
                    string value = exact.ValueKind == JsonValueKind.String
                        ? exact.GetString()
                        : exact.ToString();

                    _logger?.LogInformation(
                        "Found EXACT property. Key={Key}, Mapping={Mapping}, Value={Value}",
                        key, mapping, value);

                    return value;
                }

                // Nested property
                JsonElement current = root;
                bool found = true;

                foreach (var part in mapping.Split('.'))
                {
                    if (current.ValueKind != JsonValueKind.Object ||
                        !current.TryGetProperty(part, out current))
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    string value = current.ValueKind == JsonValueKind.String
                        ? current.GetString()
                        : current.ToString();

                    _logger?.LogInformation(
                        "Found NESTED property. Key={Key}, Mapping={Mapping}, Value={Value}",
                        key, mapping, value);

                    return value;
                }

                _logger?.LogInformation(
                    "Mapping not found. Key={Key}, Mapping={Mapping}",
                    key, mapping);
            }

            _logger?.LogWarning("No value found for key '{Key}'", key);

            return null;
        }

        public DateTime ParseTimestamp(JsonElement root)
        {
            string ts = GetMappedJsonString(root, "timestamp");
            return DateTime.TryParse(ts, out var dt) ? dt : DateTime.UtcNow;
        }

        public (string hostname, string circleCode, string domain) ParseUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return (null, null, null);

            // ── Pattern 1: host/HOSTNAME.CIRCLE.domain ────────────────────────────
            var hostMatch = Regex.Match(username, @"host/([^.]+)\.([A-Z]+)\.(.+)$", RegexOptions.IgnoreCase);
            if (hostMatch.Success)
            {
                return (
                    hostMatch.Groups[1].Value,
                    hostMatch.Groups[2].Value.ToUpper(),
                    hostMatch.Groups[3].Value
                );
            }

            // ── Pattern 2: localpart@CIRCLE.domain ───────────────────────────────
            var atMatch = Regex.Match(username, @"^([^@]+)@([A-Z]+)\.(.+)$", RegexOptions.IgnoreCase);
            if (atMatch.Success)
            {
                return (
                    atMatch.Groups[1].Value,
                    atMatch.Groups[2].Value.ToUpper(),
                    atMatch.Groups[3].Value
                );
            }

            // ── Pattern 3: CIRCLE\username (exactly 3 alpha chars) ───────────────
            var backslashMatch = Regex.Match(username, @"^([A-Z]{3})\\(.+)$", RegexOptions.IgnoreCase);
            if (backslashMatch.Success)
            {
                return (
                    backslashMatch.Groups[2].Value,
                    backslashMatch.Groups[1].Value.ToUpper(),
                    null
                );
            }

            // ── Pattern 4: no circle code ─────────────────────────────────────────
            return (username, "GLOBAL", null);
        }

        public string ExtractAdServer(string alerts)
        {
            if (string.IsNullOrEmpty(alerts)) return null;

            // Extract AD server name from alerts
            // Example: "ROOTDCMDXYZ02.AD.XYZ" from "Global Catalog for Root AD - ROOTDCMDXYZ02.AD.XYZ"
            var match = Regex.Match(alerts, @"(?:Root AD - |on |failed on )([A-Z0-9]+\.AD\.[A-Z0-9]+)", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value;

            // Look for any server pattern
            match = Regex.Match(alerts, @"([A-Z0-9]+\.AD\.[A-Z0-9]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        public string ExtractNodeIp(string alerts, string fallbackIp)
        {
            if (string.IsNullOrEmpty(alerts)) return fallbackIp;

            // Extract node IP from cluster sync alerts
            // Example: "Subscriber node 10.179.69.171"
            var match = Regex.Match(alerts, @"(?:node |Subscriber )([\d\.]+)");
            return match.Success ? match.Groups[1].Value : fallbackIp;
        }

        //old code//
        //public (string category, string reason, string isolationState) ParseQuarantineInfo(string alerts, int? errorCode)
        //{
        //    if (string.IsNullOrEmpty(alerts))
        //        return (null, null, null);

        //    string category = null;
        //    string reason = null;
        //    string isolationState = "Accepted";

        //    // Check for quarantine conditions
        //    if (errorCode == 226 || alerts.Contains("account expired") || alerts.Contains("account disabled"))
        //    {
        //        category = "AD_Issue";
        //        reason = "Account Disabled";
        //        isolationState = "Isolated";
        //    }
        //    else if (errorCode == 216 || alerts.Contains("User not found"))
        //    {
        //        category = "AD_Issue";
        //        reason = "User Not Found";
        //        isolationState = "Isolated";
        //    }
        //    else if (errorCode == 206 || alerts.Contains("Failed to get value"))
        //    {
        //        category = "Other";
        //        reason = "Authentication Failed";
        //        isolationState = "Isolated";
        //    }
        //    else if (alerts.Contains("Applied 'Reject'") || alerts.Contains("failed"))
        //    {
        //        category = "Other";
        //        reason = "Authentication Failed";
        //        isolationState = "Isolated";
        //    }
        //    else if (alerts.Contains("Applied 'Accept'") || alerts.Contains("authenticated successfully"))
        //    {
        //        reason = "Success";
        //        isolationState = "Accepted";
        //    }

        //    return (category, reason, isolationState);
        //}

        public (string category, string reason, string isolationState)ParseQuarantineInfo(string alerts, int? errorCode)
        {
            if (string.IsNullOrWhiteSpace(alerts))
                return (null, null, null);

            string alertText = alerts.ToLowerInvariant();
            string category = null;
            string reason = null;
            string isolationState = "Accepted";

            // =====================================================
            // AD ISSUES
            // =====================================================

            // Account Disabled / Expired
            if (errorCode == 226 || alertText.Contains("account disabled") || alertText.Contains("account expired") || alertText.Contains("user account expired/disabled"))
            {
                category = "AD_Issue";
                reason = "Account Disabled";
                isolationState = "Isolated";
            }

            // User Not Found
            // Covers:
            // ErrorCode=206 (MAB)
            // ErrorCode=216 (802.1x)
            // Any future variations
            else if (errorCode == 206 ||errorCode == 216 || alertText.Contains("user not found"))
            {
                category = "AD_Issue";
                reason = "User Not Found";
                isolationState = "Isolated";
            }

            // Certificate Issues
            else if (alertText.Contains("certificate"))
            {
                category = "AD_Issue";
                reason = "Certificate Invalid";
                isolationState = "Isolated";
            }
            // =====================================================
            // OTHER ISSUES
            // =====================================================

            else if (
                alertText.Contains("authentication failed") ||
                alertText.Contains("password change failed") ||
                alertText.Contains("failed to get value") ||
                alertText.Contains("applied 'reject'") ||
                alertText.Contains("applied reject")
            )
            {
                category = "Other";
                reason = "Authentication Failed";
                isolationState = "Isolated";
            }

            // =====================================================
            // SUCCESS
            // =====================================================

            else if (
                alertText.Contains("authenticated successfully") ||
                alertText.Contains("applied 'accept'") ||
                alertText.Contains("applied accept")
            )
            {
                category = null;
                reason = "Success";
                isolationState = "Accepted";
            }

            return (category, reason, isolationState);
        }

        public (bool installed, bool updated, bool scanned, string product, string reason, string category) ParseAVStatus(string antivirusInput)
        {
            if (string.IsNullOrWhiteSpace(antivirusInput))
            {
                return (
                    false,
                    false,
                    false,
                    null,
                    null,
                    null
                );
            }

            try
            {
                var avProducts = JsonConvert.DeserializeObject<Dictionary<string, AntivirusInfo>>(antivirusInput);

                if (avProducts == null || avProducts.Count == 0)
                {
                    return (
                        false,
                        false,
                        false,
                        null,
                        _avCheckMessages["AppNotInstalled"],
                        "AV_Issue"
                    );
                }

                bool installed = true;
                bool updated = true;
                bool scanned = true;

                string product = null;
                var reasons = new HashSet<string>();

                foreach (var av in avProducts)
                {
                    _logger.LogInformation(@"Product={Product} LastScan={LastScan} DatTime={DatTime} Engine={Engine} Version={Version}", av.Key, av.Value.last_scan_time, av.Value.dat_file_time,
                     av.Value.engine_version, av.Value.version);
                    // Prefer Trend Micro if available, otherwise take the first product
                    if (product == null ||
                        av.Key.Contains("Trend Micro", StringComparison.OrdinalIgnoreCase))
                    {
                        product = av.Key;
                    }

                    AntivirusInfo info = av.Value;

                    // Last Scan Check
                    if (string.IsNullOrWhiteSpace(info.last_scan_time))
                    {
                        scanned = false;
                        reasons.Add(_avCheckMessages["LastScanTimeCheck"]);
                    }

                    // DAT File Check
                    if (string.IsNullOrWhiteSpace(info.dat_file_time))
                    {
                        updated = false;
                        reasons.Add(_avCheckMessages["DatFileTimeCheck"]);
                    }

                    // Engine Version Check
                    if (string.IsNullOrWhiteSpace(info.engine_version))
                    {
                        updated = false;
                        reasons.Add(_avCheckMessages["EngineVersionCheck"]);
                    }

                    // Product Version Check
                    if (string.IsNullOrWhiteSpace(info.version))
                    {
                        updated = false;
                        reasons.Add(_avCheckMessages["ProductVersionCheck"]);
                    }
                }

                string category = reasons.Any() ? "AV_Issue" : null;

                return (
                    installed,
                    updated,
                    scanned,
                    product,
                    reasons.Any() ? string.Join(", ", reasons) : null,
                    category
                );
            }
            catch (Exception)
            {
                return (
                    false,
                    false,
                    false,
                    null,
                    _avCheckMessages["AppNotInstalled"],
                    "AV_Issue"
                );
            }
        }

        //public (bool installed, bool updated, bool scanned, string product) ParseAVStatus(string alerts)
        //{
        //    // In production, this would parse actual AV status from alerts
        //    // For now, using defaults since sample logs don't contain AV info
        //    bool installed = false;
        //    bool updated = false;
        //    bool scanned = false;
        //    string product = null;

        //    // TODO: Add actual AV parsing logic when available in logs
        //    // Example patterns to look for:
        //    // - "AV: Windows Defender installed"
        //    // - "AV: Last update 2024-01-15"
        //    // - "AV: Last scan 2024-01-20"

        //    return (installed, updated, scanned, product);
        //}

        //public (bool installed, bool updated, bool scanned, string product) ParseAVStatus(string alerts)
        //{
        //    if (string.IsNullOrWhiteSpace(alerts))
        //        return (false, false, false, null);

        //    string alertText = alerts.ToLowerInvariant();

        //    // Only set what we can actually observe from real logs.
        //    // "Multiple AntiVirus Products Detected" implies AV IS installed (just >1 product).
        //    bool? installed = null;
        //    bool updated = false;  // unknown - no pattern seen yet
        //    bool scanned = false;  // unknown - no pattern seen yet
        //    string product = null;

        //    if (alertText.Contains("multiple antivirus products"))
        //    {
        //        installed = true;
        //        var match = Regex.Match(alerts, @"AntiVirus products:\s*\[(.+?)\]", RegexOptions.IgnoreCase);
        //        if (match.Success)
        //            product = match.Groups[1].Value.Trim();
        //    }
        //    else if (alertText.Contains("antivirus not installed"))
        //    {
        //        installed = false;
        //    }

        //    // TODO: add "not updated"/"not scanned" once such alert text is observed in real logs

        //    return (installed ?? false, updated, scanned, product);
        //}

        public (bool userFound, bool accountDisabled, bool certValid, string domain)ParseADStatus(string alerts, int? errorCode)
        {
            bool userFound = true;
            bool accountDisabled = false;
            bool certValid = true;
            string domain = null;

            if (!string.IsNullOrWhiteSpace(alerts))
            {
                string alertText = alerts.ToLowerInvariant();

                // User Not Found
                if (errorCode == 206 || errorCode == 216 || alertText.Contains("user not found"))
                {
                    userFound = false;
                }

                // Account Disabled
                if (errorCode == 226 || alertText.Contains("account disabled") || alertText.Contains("account expired") || alertText.Contains("user account expired/disabled"))
                {
                    accountDisabled = true;
                }

                // Certificate Issue
                if (alertText.Contains("certificate"))
                {
                    certValid = false;
                }

                // Extract AD Domain
                var match = Regex.Match(
                    alerts,
                    @"([A-Z0-9]+\.AD\.[A-Z0-9]+)",
                    RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    domain = match.Groups[1].Value;
                }
            }

            return (userFound, accountDisabled, certValid, domain);
        }

        //---old code---
        //public string DetermineHealthStatus(int? errorCode, string alerts, string isolationState)
        //{
        //    if (isolationState == "Accepted" || errorCode == 0 ||
        //        (alerts?.Contains("authenticated successfully") ?? false))
        //    {
        //        return "Healthy";
        //    }
        //    return "Unhealthy";
        //}

        //public string DetermineHealthStatus(string postureToken, string antivirusApt, int? errorCode)
        //{
        //    string token = postureToken?.Trim().ToUpperInvariant();

        //    // Treat empty string same as null — not reported
        //    if (!string.IsNullOrEmpty(token))
        //    {
        //        if (token == "HEALTHY") return "Healthy";
        //        if (token == "QUARANTINE") return "Unhealthy";
        //        if (token == "UNKNOWN") return "Unhealthy";
        //    }

        //    string apt = antivirusApt?.Trim().ToUpperInvariant();
        //    if (!string.IsNullOrEmpty(apt))
        //    {
        //        if (apt == "HEALTHY") return "Healthy";
        //        if (apt == "QUARANTINE") return "Unhealthy";
        //        if(apt == "UNKNOWN") return "Unhealthy";
        //    }

        //    if (errorCode == 0) return "Healthy";
        //    if (errorCode.HasValue) return "Unhealthy";

        //    return "Unknown";
        //}

        public string DetermineHealthStatus(string postureToken, string antivirusApt, int? errorCode, string enforcement)
        {
            // Authentication logs
            if (!string.IsNullOrWhiteSpace(enforcement))
            {
                if (enforcement.Contains("Allow", StringComparison.OrdinalIgnoreCase))
                    return "Healthy";

                if (enforcement.Contains("Deny", StringComparison.OrdinalIgnoreCase))
                    return "Unhealthy";
            }

            // Posture Token
            string token = postureToken?.Trim().ToUpperInvariant();

            if (!string.IsNullOrEmpty(token))
            {
                if (token == "HEALTHY")
                    return "Healthy";

                if (token == "QUARANTINE" || token == "UNKNOWN")
                    return "Unhealthy";
            }

            // AV Logs
            string apt = antivirusApt?.Trim().ToUpperInvariant();

            if (!string.IsNullOrEmpty(apt))
            {
                if (apt == "HEALTHY")
                    return "Healthy";

                if (apt == "QUARANTINE" || apt == "UNKNOWN")
                    return "Unhealthy";
            }

            // Authentication fallback
            if (errorCode == 0)
                return "Healthy";

            if (errorCode.HasValue)
                return "Unhealthy";

            return "Unknown";
        }

        //-- old parsing logic --
        //public string DetermineAuthType(string username, string macAddress)
        //{
        //    // If username equals MAC address (without colons), it's MAB
        //    bool isMab = username?.Replace(":", "")
        //        .Equals(macAddress?.Replace(":", ""), StringComparison.OrdinalIgnoreCase) ?? false;

        //    return isMab ? "MAB" : "802.1x";
        //}

        public string DetermineAuthType(string service)
        {
            if (service.Contains("MAC_Auth", StringComparison.OrdinalIgnoreCase))
                return "MAB";

            if (service.Contains("WebAuth", StringComparison.OrdinalIgnoreCase))
                return "WebAuth";

            if (service.Contains("802.1x", StringComparison.OrdinalIgnoreCase))
                return "802.1x";

            return "Unknown";
        }

        //public string DetermineAuthMethod(string alerts)
        //{
        //    if (string.IsNullOrEmpty(alerts))
        //        return "Dot1x";

        //    if (alerts.Contains("EAP-TLS"))
        //        return "EAP-TLS";
        //    else if (alerts.Contains("PEAP"))
        //        return "PEAP";
        //    else if (alerts.Contains("EAP-MSCHAPv2") || alerts.Contains("MSCHAP"))
        //        return "EAP-MSCHAPv2";
        //    else if (alerts.Contains("MAC_Auth"))
        //        return "MAB";

        //    return "Dot1x";
        //}

        public int? ExtractErrorCode(string errorCodeStr, string alerts)
        {
            // First try parsing the error code field
            if (int.TryParse(errorCodeStr, out int ec))
                return ec;

            // If not available, try extracting from alerts
            if (!string.IsNullOrEmpty(alerts))
            {
                var match = Regex.Match(alerts, @"Error-Code[=:]?\s*(\d+)", RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int alertEc))
                    return alertEc;
            }

            return null;
        }

        public (float cpuUsage, float memoryUsage) ParseResourceMetrics(JsonElement root)
        {
            // Try to extract CPU and Memory usage from metadata if available
            float cpuUsage = 0f;
            float memoryUsage = 0f;

            // Check for CPU usage in various possible fields
            string cpuStr = GetJsonString(root, "CpuUsage") ??
                           GetJsonString(root, "cpu_usage") ??
                           GetJsonString(root, "metrics", "cpu");

            if (float.TryParse(cpuStr, out float cpu))
                cpuUsage = cpu;

            // Check for Memory usage
            string memStr = GetJsonString(root, "MemoryUsage") ??
                           GetJsonString(root, "memory_usage") ??
                           GetJsonString(root, "metrics", "memory");

            if (float.TryParse(memStr, out float mem))
                memoryUsage = mem;

            return (cpuUsage, memoryUsage);
        }

        public string ExtractNodeName(JsonElement root, string nodeIp)
        {
            // Try to get node name from JSON, fallback to IP-based name
            string nodeName = GetJsonString(root, "NodeName") ??
                             GetJsonString(root, "node_name") ??
                             GetJsonString(root, "hostname");

            if (string.IsNullOrEmpty(nodeName) && !string.IsNullOrEmpty(nodeIp))
            {
                // Generate a name based on IP if not available
                nodeName = $"Node-{nodeIp.Replace(".", "-")}";
            }

            return nodeName;
        }

        public string DetermineReplicationStatus(string syncStatus, string alerts)
        {
            if (string.IsNullOrEmpty(alerts))
                return "Active";

            if (alerts.Contains("successfully synchronized") ||
                alerts.Contains("completed") ||
                syncStatus?.Equals("Healthy", StringComparison.OrdinalIgnoreCase) == true)
            {
                return "Active";
            }
            else if (alerts.Contains("degraded") || alerts.Contains("warning"))
            {
                return "Degraded";
            }
            else if (alerts.Contains("failed") || alerts.Contains("error"))
            {
                return "Failed";
            }

            return "Unknown";
        }
    }
}

