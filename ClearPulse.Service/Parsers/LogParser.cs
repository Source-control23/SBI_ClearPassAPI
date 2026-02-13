using System;
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

        public DateTime ParseTimestamp(JsonElement root)
        {
            string ts = GetJsonString(root, "@timestamp") ?? GetJsonString(root, "Common.Request-Timestamp");
            return DateTime.TryParse(ts, out var dt) ? dt : DateTime.UtcNow;
        }

        public (string hostname, string circleCode, string branchCode, string domain) ParseUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return (null, "GLOBAL", null, null);

            // Examples: 
            // host/I12023D103.BHU.AD.XYZ => hostname=I12023D103, circleCode=BHU, domain=AD.XYZ
            // host/i03560d153.PAT.AD.XYZ => hostname=i03560d153, circleCode=PAT, domain=AD.XYZ

            var match = Regex.Match(username, @"host/([^.]+)\.([A-Z]{3})\.", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string hostname = match.Groups[1].Value;
                string circleCode = match.Groups[2].Value.ToUpper();

                // Extract branchCode from hostname if it contains digits
                string branchCode = null;
                var branchMatch = Regex.Match(hostname, @"(\d{5})");
                if (branchMatch.Success)
                {
                    branchCode = branchMatch.Groups[1].Value;
                }

                // Extract domain (everything after first dot)
                int firstDot = username.IndexOf('.');
                string domain = firstDot > 0 ? username.Substring(firstDot + 1) : null;

                return (hostname, circleCode, branchCode, domain);
            }

            return (username, "GLOBAL", null, null);
        }

        public string ExtractAdServer(string alerts)
        {
            if (string.IsNullOrEmpty(alerts)) return null;

            // Extract AD server name from alerts
            // Example: "ROOTDCMDXYZ02.AD.XYZ" from "Global Catalog for Root AD - ROOTDCMDXYZ02.AD.XYZ"
            var match = Regex.Match(alerts, @"(?:Root AD - |on |failed on )([A-Z0-9]+\.AD\.XYZ)", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value;

            // Look for any server pattern
            match = Regex.Match(alerts, @"([A-Z0-9]+\.AD\.XYZ)", RegexOptions.IgnoreCase);
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

        public (string category, string reason, string isolationState) ParseQuarantineInfo(string alerts, int? errorCode)
        {
            if (string.IsNullOrEmpty(alerts))
                return (null, null, null);

            string category = null;
            string reason = null;
            string isolationState = "Accepted";

            // Check for quarantine conditions
            if (errorCode == 226 || alerts.Contains("account expired") || alerts.Contains("account disabled"))
            {
                category = "AD_Issue";
                reason = "Account Disabled";
                isolationState = "Isolated";
            }
            else if (errorCode == 216 || alerts.Contains("User not found"))
            {
                category = "AD_Issue";
                reason = "User Not Found";
                isolationState = "Isolated";
            }
            else if (errorCode == 206 || alerts.Contains("Failed to get value"))
            {
                category = "Other";
                reason = "Authentication Failed";
                isolationState = "Isolated";
            }
            else if (alerts.Contains("Applied 'Reject'") || alerts.Contains("failed"))
            {
                category = "Other";
                reason = "Authentication Failed";
                isolationState = "Isolated";
            }
            else if (alerts.Contains("Applied 'Accept'") || alerts.Contains("authenticated successfully"))
            {
                reason = "Success";
                isolationState = "Accepted";
            }

            return (category, reason, isolationState);
        }

        public (bool installed, bool updated, bool scanned, string product) ParseAVStatus(string alerts)
        {
            // In production, this would parse actual AV status from alerts
            // For now, using defaults since sample logs don't contain AV info
            bool installed = false;
            bool updated = false;
            bool scanned = false;
            string product = null;

            // TODO: Add actual AV parsing logic when available in logs
            // Example patterns to look for:
            // - "AV: Windows Defender installed"
            // - "AV: Last update 2024-01-15"
            // - "AV: Last scan 2024-01-20"

            return (installed, updated, scanned, product);
        }

        public (bool userFound, bool accountDisabled, bool certValid, string domain) ParseADStatus(string alerts, int? errorCode)
        {
            bool userFound = true;
            bool accountDisabled = false;
            bool certValid = true;
            string domain = null;

            if (errorCode == 216 || alerts.Contains("User not found"))
            {
                userFound = false;
            }

            if (errorCode == 226 || alerts.Contains("account expired") || alerts.Contains("account disabled"))
            {
                accountDisabled = true;
            }

            // Extract domain from alerts
            var match = Regex.Match(alerts, @"([A-Z0-9]+\.AD\.XYZ)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                domain = match.Groups[1].Value;
            }

            return (userFound, accountDisabled, certValid, domain);
        }

        public string DetermineHealthStatus(int? errorCode, string alerts, string isolationState)
        {
            if (isolationState == "Accepted" || errorCode == 0 ||
                (alerts?.Contains("authenticated successfully") ?? false))
            {
                return "Healthy";
            }
            return "Unhealthy";
        }

        public string DetermineAuthType(string username, string macAddress)
        {
            // If username equals MAC address (without colons), it's MAB
            bool isMab = username?.Replace(":", "")
                .Equals(macAddress?.Replace(":", ""), StringComparison.OrdinalIgnoreCase) ?? false;

            return isMab ? "MAB" : "802.1x";
        }

        public string DetermineAuthMethod(string alerts)
        {
            if (string.IsNullOrEmpty(alerts))
                return "Dot1x";

            if (alerts.Contains("EAP-TLS"))
                return "EAP-TLS";
            else if (alerts.Contains("PEAP"))
                return "PEAP";
            else if (alerts.Contains("EAP-MSCHAPv2") || alerts.Contains("MSCHAP"))
                return "EAP-MSCHAPv2";
            else if (alerts.Contains("MAB"))
                return "MAB";

            return "Dot1x";
        }

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

