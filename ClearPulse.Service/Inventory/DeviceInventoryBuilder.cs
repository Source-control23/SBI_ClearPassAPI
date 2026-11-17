using System;
using System.Collections.Generic;
using System.Linq;
using ClearPulse.Core.Entities;

namespace ClearPulse.Service.Inventory
{
    /// <summary>
    /// Builds and aggregates device inventory from authentication events
    /// Handles de-duplication and merging of device data
    /// </summary>
    public class DeviceInventoryBuilder
    {
        /// <summary>
        /// Builds device inventory records from authentication events
        /// Aggregates multiple auth events per device into a single inventory record
        /// </summary>
        public List<DeviceInventory> BuildFromAuthEvents(List<DeviceAuthEvent> authEvents, Dictionary<string, string> macIpLookup = null)
        {
            if (authEvents == null || authEvents.Count == 0)
                return new List<DeviceInventory>();

            // Group by MAC address and TenantId to aggregate data per device
            var deviceGroups = authEvents
                .Where(e => !string.IsNullOrEmpty(e.MacAddress))
                .GroupBy(e => new { e.MacAddress, e.TenantId })
                .ToList();

            var inventoryRecords = new List<DeviceInventory>();

            foreach (var group in deviceGroups)
            {
                // Get the most recent event for this device
                var latestEvent = group.OrderByDescending(e => e.EventTime).First();

                // Get last successful auth
                var lastSuccess = group
                    .Where(e => e.Status == "Accept" && e.ErrorCode == 0)
                    .OrderByDescending(e => e.EventTime)
                    .FirstOrDefault();

                // Get last failed auth
                var lastFailure = group
                    .Where(e => e.Status != "Accept" || e.ErrorCode != 0)
                    .OrderByDescending(e => e.EventTime)
                    .FirstOrDefault();

                // Determine compliance status
                var complianceIssues = DetermineComplianceIssues(latestEvent);
                bool isCompliant = string.IsNullOrEmpty(complianceIssues);

                // ← ADD: look up the IP captured for this MAC during log parsing
                string resolvedIp = null;
                macIpLookup?.TryGetValue(group.Key.MacAddress, out resolvedIp);

                var inventory = new DeviceInventory
                {
                    MacAddress = group.Key.MacAddress,
                    Hostname = latestEvent.Hostname,
                    IpAddress = resolvedIp, // Would need to be extracted from NAS/RADIUS if available
                    DeviceType = DetermineDeviceType(latestEvent.Hostname, latestEvent.Username),
                    OperatingSystem = DetermineOperatingSystem(latestEvent.AuthMethod, latestEvent.Username),
                    OSVersion = latestEvent.WindowsPatchLevel,
                    BranchCode = latestEvent.BranchCode,
                    BranchName = null, // Would need branch master lookup
                    CurrentHealthStatus = latestEvent.HealthStatus,
                    LastSeenOnline = latestEvent.EventTime,
                    LastAuthSuccess = lastSuccess?.EventTime,
                    LastAuthFailure = lastFailure?.EventTime,
                    AVInstalled = latestEvent.AVInstalled,
                    AVProduct = latestEvent.AVProduct,
                    AVLastUpdate = null, // Would need to parse from posture data
                    AVLastScan = null,  // Would need to parse from posture data
                    IsCompliant = isCompliant,
                    ComplianceIssues = complianceIssues,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    SourceSystem = latestEvent.SourceSystem,
                    EventType = "Inventory",
                    CircleCode = latestEvent.CircleCode,
                    EventTime = latestEvent.EventTime,
                    TenantId = latestEvent.TenantId,
                    Status = latestEvent.Status,
                    MetadataJson = latestEvent.MetadataJson
                };

                inventoryRecords.Add(inventory);
            }

            return inventoryRecords;
        }

        /// <summary>
        /// Determines device type based on hostname and username patterns
        /// </summary>
        private string DetermineDeviceType(string hostname, string username)
        {
            if (string.IsNullOrEmpty(hostname) && string.IsNullOrEmpty(username))
                return "Unknown";

            string identifier = (hostname ?? username).ToUpper();

            // Desktop/Workstation patterns
            if (identifier.StartsWith("I") || identifier.StartsWith("D") ||
                identifier.Contains("DESKTOP") || identifier.Contains("PC"))
                return "Desktop";

            // Laptop patterns
            if (identifier.StartsWith("L") || identifier.Contains("LAPTOP") ||
                identifier.Contains("NB"))
                return "Laptop";

            // Server patterns
            if (identifier.Contains("SERVER") || identifier.Contains("SRV"))
                return "Server";

            // Printer patterns
            if (identifier.Contains("PRINT") || identifier.Contains("HP") ||
                identifier.Contains("CANON"))
                return "Printer";

            // Phone/Mobile patterns
            if (identifier.Contains("PHONE") || identifier.Contains("MOBILE") ||
                identifier.Contains("ANDROID") || identifier.Contains("IPHONE"))
                return "Mobile";

            // Access Point patterns
            if (identifier.Contains("AP") || identifier.Contains("ARUBA"))
                return "Access Point";

            return "Workstation";
        }

        /// <summary>
        /// Determines OS based on authentication method and username patterns
        /// </summary>
        private string DetermineOperatingSystem(string authMethod, string username)
        {
            if (string.IsNullOrEmpty(authMethod) && string.IsNullOrEmpty(username))
                return "Unknown";

            // Windows domain authentication patterns
            if (!string.IsNullOrEmpty(username) &&
                (username.Contains(".AD.") || username.StartsWith("host/")))
                return "Windows";

            // MAC/Apple patterns
            if (authMethod?.Contains("PEAP") == true && username?.Contains("@") == true)
                return "macOS/iOS";

            // Linux patterns
            if (authMethod?.Contains("EAP-TLS") == true)
                return "Linux";

            return "Windows"; // Default assumption for enterprise
        }

        /// <summary>
        /// Determines compliance issues based on device auth event data
        /// </summary>
        private string DetermineComplianceIssues(DeviceAuthEvent authEvent)
        {
            var issues = new List<string>();

            // AD compliance
            if (!authEvent.ADUserFound)
                issues.Add("AD User Not Found");

            if (authEvent.ADAccountDisabled)
                issues.Add("AD Account Disabled");

            if (!authEvent.ADCertificateValid)
                issues.Add("Invalid Certificate");

            // AV compliance
            if (!authEvent.AVInstalled)
                issues.Add("Antivirus Not Installed");
            else if (!authEvent.AVUpdated)
                issues.Add("Antivirus Out of Date");
            else if (!authEvent.AVScanned)
                issues.Add("No Recent AV Scan");

            // Patch compliance
            if (authEvent.CriticalPatchesMissing.HasValue && authEvent.CriticalPatchesMissing > 0)
                issues.Add($"Missing {authEvent.CriticalPatchesMissing} Critical Patches");

            // Quarantine status
            if (!string.IsNullOrEmpty(authEvent.QuarantineCategory))
                issues.Add($"Quarantined: {authEvent.QuarantineReason}");

            return issues.Count > 0 ? string.Join("; ", issues) : null;
        }

        /// <summary>
        /// Merges new inventory data with existing records
        /// Used when you want to update in-memory before database operations
        /// </summary>
        public List<DeviceInventory> MergeInventoryRecords(
            List<DeviceInventory> existing,
            List<DeviceInventory> newRecords)
        {
            var merged = existing.ToDictionary(
                e => new { e.MacAddress, e.TenantId },
                e => e);

            foreach (var newRecord in newRecords)
            {
                var key = new { newRecord.MacAddress, newRecord.TenantId };

                if (merged.ContainsKey(key))
                {
                    // Update existing record
                    var existingRecord = merged[key];
                    UpdateInventoryRecord(existingRecord, newRecord);
                }
                else
                {
                    // Add new record
                    merged[key] = newRecord;
                }
            }

            return merged.Values.ToList();
        }

        /// <summary>
        /// Updates an existing inventory record with new data
        /// Preserves historical data and only updates changed fields
        /// </summary>
        private void UpdateInventoryRecord(DeviceInventory existing, DeviceInventory newData)
        {
            // Update only if new data is more recent
            if (newData.EventTime <= existing.EventTime)
                return;

            // Update fields that may change
            existing.Hostname = newData.Hostname ?? existing.Hostname;
            existing.IpAddress = newData.IpAddress ?? existing.IpAddress;
            existing.CurrentHealthStatus = newData.CurrentHealthStatus;
            existing.LastSeenOnline = newData.EventTime;

            // Update last success time if this was a successful auth
            if (newData.LastAuthSuccess.HasValue &&
                (!existing.LastAuthSuccess.HasValue || newData.LastAuthSuccess > existing.LastAuthSuccess))
            {
                existing.LastAuthSuccess = newData.LastAuthSuccess;
            }

            // Update last failure time if this was a failed auth
            if (newData.LastAuthFailure.HasValue &&
                (!existing.LastAuthFailure.HasValue || newData.LastAuthFailure > existing.LastAuthFailure))
            {
                existing.LastAuthFailure = newData.LastAuthFailure;
            }

            // Update AV information if available
            if (newData.AVInstalled.HasValue)
            {
                existing.AVInstalled = newData.AVInstalled;
                existing.AVProduct = newData.AVProduct ?? existing.AVProduct;
                existing.AVLastUpdate = newData.AVLastUpdate ?? existing.AVLastUpdate;
                existing.AVLastScan = newData.AVLastScan ?? existing.AVLastScan;
            }

            // Update compliance status
            existing.IsCompliant = newData.IsCompliant;
            existing.ComplianceIssues = newData.ComplianceIssues;

            // Update metadata
            existing.UpdatedDate = DateTime.UtcNow;
            existing.EventTime = newData.EventTime;
            existing.Status = newData.Status;
            existing.MetadataJson = newData.MetadataJson;
        }
    }
}