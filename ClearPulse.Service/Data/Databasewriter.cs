using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using ClearPulse.Core.Entities;
using Microsoft.Extensions.Logging;

namespace ClearPulse.Service.Data
{
    /// <summary>
    /// Handles all database write operations with bulk insert support
    /// </summary>
    public class DatabaseWriter
    {
        private readonly ILogger _logger;

        public DatabaseWriter(ILogger logger)
        {
            _logger = logger;
        }

        public async Task BulkInsertAsync(string tableName, List<BaseLogEntity> entities, string connectionString)
        {
            if (entities == null || entities.Count == 0)
                return;

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var dataTable = CreateDataTable(tableName, entities);

                using var bulkCopy = new SqlBulkCopy(conn)
                {
                    DestinationTableName = tableName,
                    BatchSize = 1000,
                    BulkCopyTimeout = 300
                };

                // Map columns
                foreach (DataColumn col in dataTable.Columns)
                {
                    bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                }

                await bulkCopy.WriteToServerAsync(dataTable);
                _logger.LogInformation($"Inserted {entities.Count} records into {tableName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error bulk inserting into {tableName}");
                throw;
            }
        }

        private DataTable CreateDataTable(string tableName, List<BaseLogEntity> entities)
        {
            var dt = new DataTable();

            // Define common columns first
            dt.Columns.Add("SourceSystem", typeof(string));
            dt.Columns.Add("EventType", typeof(string));
            dt.Columns.Add("CircleCode", typeof(string));
            dt.Columns.Add("Status", typeof(string));
            dt.Columns.Add("EventTime", typeof(DateTime));
            dt.Columns.Add("MetadataJson", typeof(string));
            dt.Columns.Add("TenantId", typeof(string));

            // Add table-specific columns
            switch (tableName)
            {
                case "AdIntegrations":
                    dt.Columns.Add("AdServer", typeof(string));
                    dt.Columns.Add("DomainName", typeof(string));
                    dt.Columns.Add("FailureReason", typeof(string));
                    dt.Columns.Add("ErrorCode", typeof(int)).AllowDBNull = true;
                    break;

                case "ClusterNodes":
                    dt.Columns.Add("NodeIp", typeof(string));
                    dt.Columns.Add("NodeName", typeof(string));
                    dt.Columns.Add("Site", typeof(string));
                    dt.Columns.Add("SyncStatus", typeof(string));
                    dt.Columns.Add("ReplicationStatus", typeof(string));
                    dt.Columns.Add("Uptime", typeof(string));
                    dt.Columns.Add("NodeRole", typeof(string));
                    dt.Columns.Add("CpuUsage", typeof(float));
                    dt.Columns.Add("MemoryUsage", typeof(float));
                    break;

                case "DeviceAuthEvents":
                    dt.Columns.Add("MacAddress", typeof(string));
                    dt.Columns.Add("Username", typeof(string));
                    dt.Columns.Add("Hostname", typeof(string));
                    dt.Columns.Add("NasIp", typeof(string));
                    dt.Columns.Add("AuthType", typeof(string));
                    dt.Columns.Add("AuthMethod", typeof(string));
                    dt.Columns.Add("ErrorCode", typeof(int)).AllowDBNull = true;
                    dt.Columns.Add("Reason", typeof(string));
                    dt.Columns.Add("EnforcementProfile", typeof(string));
                    dt.Columns.Add("BranchCode", typeof(string));
                    dt.Columns.Add("QuarantineReason", typeof(string));
                    dt.Columns.Add("QuarantineCategory", typeof(string));
                    dt.Columns.Add("IsolationState", typeof(string));
                    dt.Columns.Add("AVInstalled", typeof(bool));
                    dt.Columns.Add("AVUpdated", typeof(bool));
                    dt.Columns.Add("AVScanned", typeof(bool));
                    dt.Columns.Add("AVProduct", typeof(string));
                    dt.Columns.Add("ADAccountDisabled", typeof(bool));
                    dt.Columns.Add("ADUserFound", typeof(bool));
                    dt.Columns.Add("ADCertificateValid", typeof(bool));
                    dt.Columns.Add("ADDomain", typeof(string));
                    dt.Columns.Add("WindowsPatchLevel", typeof(string));
                    dt.Columns.Add("CriticalPatchesMissing", typeof(int)).AllowDBNull = true;
                    dt.Columns.Add("LastPatchDate", typeof(DateTime)).AllowDBNull = true;
                    dt.Columns.Add("HealthStatus", typeof(string));
                    break;

                case "NasDevices":
                    dt.Columns.Add("NasIp", typeof(string));
                    dt.Columns.Add("RequestCount", typeof(int));
                    dt.Columns.Add("LastSeen", typeof(DateTime));
                    dt.Columns.Add("BranchCode", typeof(string));
                    dt.Columns.Add("BranchName", typeof(string));
                    dt.Columns.Add("DeviceModel", typeof(string));
                    dt.Columns.Add("FirmwareVersion", typeof(string));
                    dt.Columns.Add("IsActive", typeof(bool));
                    break;

                case "QuarantineDetails":
                    dt.Columns.Add("MacAddress", typeof(string));
                    dt.Columns.Add("QuarantineType", typeof(string));
                    dt.Columns.Add("QuarantineReason", typeof(string));
                    dt.Columns.Add("DetailedMessage", typeof(string));
                    dt.Columns.Add("QuarantineStartTime", typeof(DateTime));
                    dt.Columns.Add("QuarantineEndTime", typeof(DateTime)).AllowDBNull = true;
                    dt.Columns.Add("IsActive", typeof(bool));
                    dt.Columns.Add("ResolvedBy", typeof(string));
                    dt.Columns.Add("ResolutionNotes", typeof(string));
                    break;

                case "DeviceInventory":
                    dt.Columns.Add("MacAddress", typeof(string));
                    dt.Columns.Add("Hostname", typeof(string));
                    dt.Columns.Add("IpAddress", typeof(string));
                    dt.Columns.Add("OperatingSystem", typeof(string));
                    dt.Columns.Add("BranchCode", typeof(string));
                    dt.Columns.Add("CurrentHealthStatus", typeof(string));
                    dt.Columns.Add("LastSeenOnline", typeof(DateTime)).AllowDBNull = true;
                    dt.Columns.Add("LastAuthSuccess", typeof(DateTime)).AllowDBNull = true;
                    dt.Columns.Add("LastAuthFailure", typeof(DateTime)).AllowDBNull = true;
                    dt.Columns.Add("AVInstalled", typeof(bool)).AllowDBNull = true;
                    dt.Columns.Add("AVProduct", typeof(string));
                    dt.Columns.Add("IsCompliant", typeof(bool)).AllowDBNull = true;
                    dt.Columns.Add("ComplianceIssues", typeof(string));
                    dt.Columns.Add("UpdatedDate", typeof(DateTime));
                    break;
            }

            // Populate rows
            foreach (var e in entities)
            {
                var row = dt.NewRow();

                // Common fields
                row["SourceSystem"] = (object)e.SourceSystem ?? DBNull.Value;
                row["EventType"] = (object)e.EventType ?? DBNull.Value;
                row["CircleCode"] = (object)e.CircleCode ?? "GLOBAL";
                row["Status"] = (object)e.Status ?? DBNull.Value;
                row["EventTime"] = (object)e.EventTime ?? DateTime.UtcNow;
                row["MetadataJson"] = (object)e.MetadataJson ?? DBNull.Value;
                row["TenantId"] = (object)e.TenantId ?? DBNull.Value;

                // Type-specific fields
                PopulateTypeSpecificFields(row, e);

                dt.Rows.Add(row);
            }

            return dt;
        }

        private void PopulateTypeSpecificFields(DataRow row, BaseLogEntity e)
        {
            if (e is AdIntegration ad)
            {
                row["AdServer"] = (object)ad.AdServer ?? DBNull.Value;
                row["DomainName"] = (object)ad.DomainName ?? DBNull.Value;
                row["FailureReason"] = (object)ad.FailureReason ?? DBNull.Value;
                row["ErrorCode"] = (object)ad.ErrorCode ?? DBNull.Value;
            }
            else if (e is ClusterNode c)
            {
                row["NodeIp"] = (object)c.NodeIp ?? DBNull.Value;
                row["NodeName"] = (object)c.NodeName ?? DBNull.Value;
                row["Site"] = (object)c.Site ?? DBNull.Value;
                row["SyncStatus"] = (object)c.SyncStatus ?? DBNull.Value;
                row["ReplicationStatus"] = (object)c.ReplicationStatus ?? DBNull.Value;
                row["Uptime"] = (object)c.Uptime ?? DBNull.Value;
                row["NodeRole"] = (object)c.NodeRole ?? DBNull.Value;
                row["CpuUsage"] = c.CpuUsage;
                row["MemoryUsage"] = c.MemoryUsage;
            }
            else if (e is DeviceAuthEvent a)
            {
                row["MacAddress"] = (object)a.MacAddress ?? DBNull.Value;
                row["Username"] = (object)a.Username ?? DBNull.Value;
                row["Hostname"] = (object)a.Hostname ?? DBNull.Value;
                row["NasIp"] = (object)a.NasIp ?? DBNull.Value;
                row["AuthType"] = (object)a.AuthType ?? DBNull.Value;
                row["AuthMethod"] = (object)a.AuthMethod ?? DBNull.Value;
                row["ErrorCode"] = (object)a.ErrorCode ?? DBNull.Value;
                row["Reason"] = (object)a.Reason ?? DBNull.Value;
                row["EnforcementProfile"] = (object)a.EnforcementProfile ?? DBNull.Value;
                row["BranchCode"] = (object)a.BranchCode ?? DBNull.Value;
                row["QuarantineReason"] = (object)a.QuarantineReason ?? DBNull.Value;
                row["QuarantineCategory"] = (object)a.QuarantineCategory ?? DBNull.Value;
                row["IsolationState"] = (object)a.IsolationState ?? DBNull.Value;
                row["AVInstalled"] = a.AVInstalled;
                row["AVUpdated"] = a.AVUpdated;
                row["AVScanned"] = a.AVScanned;
                row["AVProduct"] = (object)a.AVProduct ?? DBNull.Value;
                row["ADAccountDisabled"] = a.ADAccountDisabled;
                row["ADUserFound"] = a.ADUserFound;
                row["ADCertificateValid"] = a.ADCertificateValid;
                row["ADDomain"] = (object)a.ADDomain ?? DBNull.Value;
                row["WindowsPatchLevel"] = (object)a.WindowsPatchLevel ?? DBNull.Value;
                row["CriticalPatchesMissing"] = (object)a.CriticalPatchesMissing ?? DBNull.Value;
                row["LastPatchDate"] = (object)a.LastPatchDate ?? DBNull.Value;
                row["HealthStatus"] = (object)a.HealthStatus ?? DBNull.Value;
            }
            else if (e is NasDevice nd)
            {
                row["NasIp"] = (object)nd.NasIp ?? DBNull.Value;
                row["RequestCount"] = nd.RequestCount;
                row["LastSeen"] = (object)nd.LastSeen ?? DateTime.UtcNow;
                row["BranchCode"] = (object)nd.BranchCode ?? DBNull.Value;
                row["BranchName"] = (object)nd.BranchName ?? DBNull.Value;
                row["DeviceModel"] = (object)nd.DeviceModel ?? DBNull.Value;
                row["FirmwareVersion"] = (object)nd.FirmwareVersion ?? DBNull.Value;
                row["IsActive"] = nd.IsActive;
            }
            else if (e is QuarantineDetail qd)
            {
                row["MacAddress"] = (object)qd.MacAddress ?? DBNull.Value;
                row["QuarantineType"] = (object)qd.QuarantineType ?? DBNull.Value;
                row["QuarantineReason"] = (object)qd.QuarantineReason ?? DBNull.Value;
                row["DetailedMessage"] = (object)qd.DetailedMessage ?? DBNull.Value;
                row["QuarantineStartTime"] = qd.QuarantineStartTime;
                row["QuarantineEndTime"] = (object)qd.QuarantineEndTime ?? DBNull.Value;
                row["IsActive"] = qd.IsActive;
                row["ResolvedBy"] = (object)qd.ResolvedBy ?? DBNull.Value;
                row["ResolutionNotes"] = (object)qd.ResolutionNotes ?? DBNull.Value;
            }
            else if (e is DeviceInventory inv)
            {
                row["MacAddress"] = inv.MacAddress;
                row["Hostname"] = (object)inv.Hostname ?? DBNull.Value;
                row["IpAddress"] = (object)inv.IpAddress ?? DBNull.Value;
                row["OperatingSystem"] = (object)inv.OperatingSystem ?? DBNull.Value;
                row["BranchCode"] = (object)inv.BranchCode ?? DBNull.Value;
                row["CurrentHealthStatus"] = (object)inv.CurrentHealthStatus ?? DBNull.Value;
                row["LastSeenOnline"] = (object)inv.LastSeenOnline ?? DBNull.Value;
                row["LastAuthSuccess"] = (object)inv.LastAuthSuccess ?? DBNull.Value;
                row["LastAuthFailure"] = (object)inv.LastAuthFailure ?? DBNull.Value;
                row["AVInstalled"] = (object)inv.AVInstalled ?? DBNull.Value;
                row["AVProduct"] = (object)inv.AVProduct ?? DBNull.Value;
                row["IsCompliant"] = (object)inv.IsCompliant ?? DBNull.Value;
                row["ComplianceIssues"] = (object)inv.ComplianceIssues ?? DBNull.Value;
                row["UpdatedDate"] = inv.UpdatedDate;
            }
        }
    }
}