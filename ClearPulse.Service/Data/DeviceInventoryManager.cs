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
    /// Manages Device Inventory with optimized UPSERT (insert or update) operations
    /// Uses SQL Server MERGE for efficient batch processing
    /// </summary>
    public class DeviceInventoryManager
    {
        private readonly ILogger _logger;

        public DeviceInventoryManager(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Upserts device inventory records using SQL MERGE statement
        /// This is optimized for bulk operations while handling updates
        /// </summary>
        public async Task UpsertDeviceInventoryAsync(List<DeviceInventory> devices, string connectionString)
        {
            if (devices == null || devices.Count == 0)
                return;

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                // Create staging table with data
                var stagingTable = CreateStagingDataTable(devices);

                // Upload to temp table
                string tempTableName = $"#TempDeviceInventory_{Guid.NewGuid():N}";
                await CreateTempTableAsync(conn, tempTableName);
                await BulkInsertToTempTableAsync(conn, tempTableName, stagingTable);

                // Execute MERGE operation
                await ExecuteMergeAsync(conn, tempTableName, devices.Count);

                _logger.LogInformation($"Upserted {devices.Count} device inventory records");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting device inventory");
                throw;
            }
        }

        /// <summary>
        /// Alternative approach: Batch update existing + insert new
        /// More control but slightly slower than MERGE
        /// </summary>
        public async Task UpsertDeviceInventoryBatchAsync(List<DeviceInventory> devices, string connectionString)
        {
            if (devices == null || devices.Count == 0)
                return;

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                // Get existing MAC addresses
                var existingMacs = await GetExistingMacAddressesAsync(conn,
                    devices.Select(d => d.MacAddress).ToList());

                // Split into updates and inserts
                var toUpdate = devices.Where(d => existingMacs.Contains(d.MacAddress)).ToList();
                var toInsert = devices.Where(d => !existingMacs.Contains(d.MacAddress)).ToList();

                // Batch update existing records
                if (toUpdate.Count > 0)
                {
                    await BatchUpdateAsync(conn, toUpdate);
                    _logger.LogInformation($"Updated {toUpdate.Count} existing devices");
                }

                // Bulk insert new records
                if (toInsert.Count > 0)
                {
                    await BulkInsertNewAsync(conn, toInsert);
                    _logger.LogInformation($"Inserted {toInsert.Count} new devices");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch upsert");
                throw;
            }
        }

        #region MERGE Strategy (Recommended - Fastest)

        private async Task CreateTempTableAsync(SqlConnection conn, string tempTableName)
        {
            string createTableSql = $@"
                CREATE TABLE {tempTableName} (
                    MacAddress NVARCHAR(12) NOT NULL,
                    Hostname NVARCHAR(MAX),
                    IpAddress NVARCHAR(MAX),
                    DeviceType NVARCHAR(MAX),
                    OperatingSystem NVARCHAR(MAX),
                    OSVersion NVARCHAR(MAX),
                    BranchCode NVARCHAR(MAX),
                    BranchName NVARCHAR(MAX),
                    CurrentHealthStatus NVARCHAR(450),
                    LastSeenOnline DATETIME2(7),
                    LastAuthSuccess DATETIME2(7),
                    LastAuthFailure DATETIME2(7),
                    AVInstalled BIT,
                    AVProduct NVARCHAR(MAX),
                    AVLastUpdate DATETIME2(7),
                    AVLastScan DATETIME2(7),
                    IsCompliant BIT,
                    ComplianceIssues NVARCHAR(MAX),
                    SourceSystem NVARCHAR(50),
                    EventType NVARCHAR(100),
                    CircleCode NVARCHAR(20),
                    EventTime DATETIME2(7) NOT NULL,
                    TenantId NVARCHAR(450),
                    Status NVARCHAR(MAX),
                    MetadataJson NVARCHAR(MAX)
                )";

            using var cmd = new SqlCommand(createTableSql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task BulkInsertToTempTableAsync(SqlConnection conn, string tempTableName, DataTable dataTable)
        {
            using var bulkCopy = new SqlBulkCopy(conn)
            {
                DestinationTableName = tempTableName,
                BatchSize = 1000
            };

            foreach (DataColumn col in dataTable.Columns)
            {
                bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
            }

            await bulkCopy.WriteToServerAsync(dataTable);
        }

        private async Task ExecuteMergeAsync(SqlConnection conn, string tempTableName, int recordCount)
        {
            string mergeSql = $@"
                MERGE INTO [dbo].[DeviceInventory] AS target
                USING {tempTableName} AS source
                ON target.MacAddress = source.MacAddress AND target.TenantId = source.TenantId
                
                -- UPDATE existing records
                WHEN MATCHED THEN
                    UPDATE SET
                        target.Hostname = COALESCE(source.Hostname, target.Hostname),
                        target.IpAddress = COALESCE(source.IpAddress, target.IpAddress),
                        target.DeviceType = COALESCE(source.DeviceType, target.DeviceType),
                        target.OperatingSystem = COALESCE(source.OperatingSystem, target.OperatingSystem),
                        target.OSVersion = COALESCE(source.OSVersion, target.OSVersion),
                        target.BranchCode = COALESCE(source.BranchCode, target.BranchCode),
                        target.BranchName = COALESCE(source.BranchName, target.BranchName),
                        target.CurrentHealthStatus = source.CurrentHealthStatus,
                        target.LastSeenOnline = source.EventTime,
                        target.LastAuthSuccess = CASE 
                            WHEN source.CurrentHealthStatus = 'Healthy' THEN source.EventTime 
                            ELSE target.LastAuthSuccess 
                        END,
                        target.LastAuthFailure = CASE 
                            WHEN source.CurrentHealthStatus = 'Unhealthy' THEN source.EventTime 
                            ELSE target.LastAuthFailure 
                        END,
                        target.AVInstalled = COALESCE(source.AVInstalled, target.AVInstalled),
                        target.AVProduct = COALESCE(source.AVProduct, target.AVProduct),
                        target.AVLastUpdate = COALESCE(source.AVLastUpdate, target.AVLastUpdate),
                        target.AVLastScan = COALESCE(source.AVLastScan, target.AVLastScan),
                        target.IsCompliant = source.IsCompliant,
                        target.ComplianceIssues = source.ComplianceIssues,
                        target.UpdatedDate = GETUTCDATE(),
                        target.Status = source.Status,
                        target.MetadataJson = source.MetadataJson
                
                -- INSERT new records
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT (
                        MacAddress, Hostname, IpAddress, DeviceType, OperatingSystem, OSVersion,
                        BranchCode, BranchName, CurrentHealthStatus, LastSeenOnline,
                        LastAuthSuccess, LastAuthFailure, AVInstalled, AVProduct, AVLastUpdate,
                        AVLastScan, IsCompliant, ComplianceIssues, CreatedDate, UpdatedDate,
                        SourceSystem, EventType, CircleCode, EventTime, TenantId, Status, MetadataJson
                    )
                    VALUES (
                        source.MacAddress, source.Hostname, source.IpAddress, source.DeviceType,
                        source.OperatingSystem, source.OSVersion, source.BranchCode, source.BranchName,
                        source.CurrentHealthStatus, source.EventTime,
                        CASE WHEN source.CurrentHealthStatus = 'Healthy' THEN source.EventTime ELSE NULL END,
                        CASE WHEN source.CurrentHealthStatus = 'Unhealthy' THEN source.EventTime ELSE NULL END,
                        source.AVInstalled, source.AVProduct, source.AVLastUpdate, source.AVLastScan,
                        source.IsCompliant, source.ComplianceIssues, GETUTCDATE(), GETUTCDATE(),
                        source.SourceSystem, source.EventType, source.CircleCode, source.EventTime,
                        source.TenantId, source.Status, source.MetadataJson
                    );";

            using var cmd = new SqlCommand(mergeSql, conn);
            cmd.CommandTimeout = 300;
            await cmd.ExecuteNonQueryAsync();
        }

        #endregion

        #region Batch Update Strategy (Alternative)

        private async Task<HashSet<string>> GetExistingMacAddressesAsync(SqlConnection conn, List<string> macAddresses)
        {
            var existingMacs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Create temp table for lookup
            string tempTable = $"#TempMacLookup_{Guid.NewGuid():N}";

            using (var cmd = new SqlCommand($@"
                CREATE TABLE {tempTable} (MacAddress NVARCHAR(12) NOT NULL);
                ", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Bulk insert MAC addresses to temp table
            var dt = new DataTable();
            dt.Columns.Add("MacAddress", typeof(string));
            foreach (var mac in macAddresses)
            {
                dt.Rows.Add(mac);
            }

            using (var bulkCopy = new SqlBulkCopy(conn) { DestinationTableName = tempTable })
            {
                bulkCopy.ColumnMappings.Add("MacAddress", "MacAddress");
                await bulkCopy.WriteToServerAsync(dt);
            }

            // Query existing MACs
            using (var cmd = new SqlCommand($@"
                SELECT DISTINCT di.MacAddress
                FROM [dbo].[DeviceInventory] di
                INNER JOIN {tempTable} tmp ON di.MacAddress = tmp.MacAddress
                ", conn))
            {
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    existingMacs.Add(reader.GetString(0));
                }
            }

            return existingMacs;
        }

        private async Task BatchUpdateAsync(SqlConnection conn, List<DeviceInventory> devices)
        {
            // Use batched UPDATE statements (SQL Server supports up to 1000 rows per batch)
            const int batchSize = 500;

            for (int i = 0; i < devices.Count; i += batchSize)
            {
                var batch = devices.Skip(i).Take(batchSize).ToList();
                await ExecuteUpdateBatchAsync(conn, batch);
            }
        }

        private async Task ExecuteUpdateBatchAsync(SqlConnection conn, List<DeviceInventory> batch)
        {
            // Build parameterized batch update
            var parameters = new List<SqlParameter>();
            var updateCases = new List<string>();

            for (int i = 0; i < batch.Count; i++)
            {
                var device = batch[i];
                string prefix = $"@p{i}_";

                parameters.Add(new SqlParameter($"{prefix}mac", device.MacAddress));
                parameters.Add(new SqlParameter($"{prefix}hostname", (object)device.Hostname ?? DBNull.Value));
                parameters.Add(new SqlParameter($"{prefix}ip", (object)device.IpAddress ?? DBNull.Value));
                parameters.Add(new SqlParameter($"{prefix}health", (object)device.CurrentHealthStatus ?? DBNull.Value));
                parameters.Add(new SqlParameter($"{prefix}eventTime", device.EventTime));
                parameters.Add(new SqlParameter($"{prefix}compliant", (object)device.IsCompliant ?? DBNull.Value));
                parameters.Add(new SqlParameter($"{prefix}status", (object)device.Status ?? DBNull.Value));

                updateCases.Add($"{prefix}mac");
            }

            string sql = $@"
                UPDATE [dbo].[DeviceInventory]
                SET 
                    CurrentHealthStatus = CASE MacAddress {string.Join(" ", batch.Select((d, idx) => $"WHEN @p{idx}_mac THEN @p{idx}_health"))} END,
                    LastSeenOnline = CASE MacAddress {string.Join(" ", batch.Select((d, idx) => $"WHEN @p{idx}_mac THEN @p{idx}_eventTime"))} END,
                    IsCompliant = CASE MacAddress {string.Join(" ", batch.Select((d, idx) => $"WHEN @p{idx}_mac THEN @p{idx}_compliant"))} END,
                    UpdatedDate = GETUTCDATE()
                WHERE MacAddress IN ({string.Join(",", batch.Select((d, idx) => $"@p{idx}_mac"))})";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task BulkInsertNewAsync(SqlConnection conn, List<DeviceInventory> devices)
        {
            var dt = CreateDeviceInventoryDataTable(devices);

            using var bulkCopy = new SqlBulkCopy(conn)
            {
                DestinationTableName = "[dbo].[DeviceInventory]",
                BatchSize = 1000
            };

            foreach (DataColumn col in dt.Columns)
            {
                bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
            }

            await bulkCopy.WriteToServerAsync(dt);
        }

        #endregion

        #region DataTable Creation

        private DataTable CreateStagingDataTable(List<DeviceInventory> devices)
        {
            var dt = new DataTable();

            // Add all columns needed for MERGE
            dt.Columns.Add("MacAddress", typeof(string));
            dt.Columns.Add("Hostname", typeof(string));
            dt.Columns.Add("IpAddress", typeof(string));
            dt.Columns.Add("DeviceType", typeof(string));
            dt.Columns.Add("OperatingSystem", typeof(string));
            dt.Columns.Add("OSVersion", typeof(string));
            dt.Columns.Add("BranchCode", typeof(string));
            dt.Columns.Add("BranchName", typeof(string));
            dt.Columns.Add("CurrentHealthStatus", typeof(string));
            dt.Columns.Add("LastSeenOnline", typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("LastAuthSuccess", typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("LastAuthFailure", typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("AVInstalled", typeof(bool)).AllowDBNull = true;
            dt.Columns.Add("AVProduct", typeof(string));
            dt.Columns.Add("AVLastUpdate", typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("AVLastScan", typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("IsCompliant", typeof(bool)).AllowDBNull = true;
            dt.Columns.Add("ComplianceIssues", typeof(string));
            dt.Columns.Add("SourceSystem", typeof(string));
            dt.Columns.Add("EventType", typeof(string));
            dt.Columns.Add("CircleCode", typeof(string));
            dt.Columns.Add("EventTime", typeof(DateTime));
            dt.Columns.Add("TenantId", typeof(string));
            dt.Columns.Add("Status", typeof(string));
            dt.Columns.Add("MetadataJson", typeof(string));

            foreach (var device in devices)
            {
                var row = dt.NewRow();
                row["MacAddress"] = device.MacAddress;
                row["Hostname"] = (object)device.Hostname ?? DBNull.Value;
                row["IpAddress"] = (object)device.IpAddress ?? DBNull.Value;
                row["DeviceType"] = (object)device.DeviceType ?? DBNull.Value;
                row["OperatingSystem"] = (object)device.OperatingSystem ?? DBNull.Value;
                row["OSVersion"] = (object)device.OSVersion ?? DBNull.Value;
                row["BranchCode"] = (object)device.BranchCode ?? DBNull.Value;
                row["BranchName"] = (object)device.BranchName ?? DBNull.Value;
                row["CurrentHealthStatus"] = (object)device.CurrentHealthStatus ?? DBNull.Value;
                row["LastSeenOnline"] = (object)device.LastSeenOnline ?? DBNull.Value;
                row["LastAuthSuccess"] = (object)device.LastAuthSuccess ?? DBNull.Value;
                row["LastAuthFailure"] = (object)device.LastAuthFailure ?? DBNull.Value;
                row["AVInstalled"] = (object)device.AVInstalled ?? DBNull.Value;
                row["AVProduct"] = (object)device.AVProduct ?? DBNull.Value;
                row["AVLastUpdate"] = (object)device.AVLastUpdate ?? DBNull.Value;
                row["AVLastScan"] = (object)device.AVLastScan ?? DBNull.Value;
                row["IsCompliant"] = (object)device.IsCompliant ?? DBNull.Value;
                row["ComplianceIssues"] = (object)device.ComplianceIssues ?? DBNull.Value;
                row["SourceSystem"] = (object)device.SourceSystem ?? DBNull.Value;
                row["EventType"] = (object)device.EventType ?? DBNull.Value;
                row["CircleCode"] = (object)device.CircleCode ?? "GLOBAL";
                row["EventTime"] = device.EventTime;
                row["TenantId"] = (object)device.TenantId ?? DBNull.Value;
                row["Status"] = (object)device.Status ?? DBNull.Value;
                row["MetadataJson"] = (object)device.MetadataJson ?? DBNull.Value;

                dt.Rows.Add(row);
            }

            return dt;
        }

        private DataTable CreateDeviceInventoryDataTable(List<DeviceInventory> devices)
        {
            var dt = new DataTable();

            dt.Columns.Add("MacAddress", typeof(string));
            dt.Columns.Add("Hostname", typeof(string));
            dt.Columns.Add("IpAddress", typeof(string));
            dt.Columns.Add("DeviceType", typeof(string));
            dt.Columns.Add("OperatingSystem", typeof(string));
            dt.Columns.Add("OSVersion", typeof(string));
            dt.Columns.Add("BranchCode", typeof(string));
            dt.Columns.Add("BranchName", typeof(string));
            dt.Columns.Add("CurrentHealthStatus", typeof(string));
            dt.Columns.Add("LastSeenOnline", typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("LastAuthSuccess", typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("LastAuthFailure", typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("AVInstalled", typeof(bool)).AllowDBNull = true;
            dt.Columns.Add("AVProduct", typeof(string));
            dt.Columns.Add("AVLastUpdate", typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("AVLastScan", typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("IsCompliant", typeof(bool)).AllowDBNull = true;
            dt.Columns.Add("ComplianceIssues", typeof(string));
            dt.Columns.Add("CreatedDate", typeof(DateTime));
            dt.Columns.Add("UpdatedDate", typeof(DateTime));
            dt.Columns.Add("SourceSystem", typeof(string));
            dt.Columns.Add("EventType", typeof(string));
            dt.Columns.Add("CircleCode", typeof(string));
            dt.Columns.Add("EventTime", typeof(DateTime));
            dt.Columns.Add("TenantId", typeof(string));
            dt.Columns.Add("Status", typeof(string));
            dt.Columns.Add("MetadataJson", typeof(string));

            foreach (var device in devices)
            {
                var row = dt.NewRow();
                row["MacAddress"] = device.MacAddress;
                row["Hostname"] = (object)device.Hostname ?? DBNull.Value;
                row["IpAddress"] = (object)device.IpAddress ?? DBNull.Value;
                row["DeviceType"] = (object)device.DeviceType ?? DBNull.Value;
                row["OperatingSystem"] = (object)device.OperatingSystem ?? DBNull.Value;
                row["OSVersion"] = (object)device.OSVersion ?? DBNull.Value;
                row["BranchCode"] = (object)device.BranchCode ?? DBNull.Value;
                row["BranchName"] = (object)device.BranchName ?? DBNull.Value;
                row["CurrentHealthStatus"] = (object)device.CurrentHealthStatus ?? DBNull.Value;
                row["LastSeenOnline"] = (object)device.LastSeenOnline ?? DBNull.Value;
                row["LastAuthSuccess"] = (object)device.LastAuthSuccess ?? DBNull.Value;
                row["LastAuthFailure"] = (object)device.LastAuthFailure ?? DBNull.Value;
                row["AVInstalled"] = (object)device.AVInstalled ?? DBNull.Value;
                row["AVProduct"] = (object)device.AVProduct ?? DBNull.Value;
                row["AVLastUpdate"] = (object)device.AVLastUpdate ?? DBNull.Value;
                row["AVLastScan"] = (object)device.AVLastScan ?? DBNull.Value;
                row["IsCompliant"] = (object)device.IsCompliant ?? DBNull.Value;
                row["ComplianceIssues"] = (object)device.ComplianceIssues ?? DBNull.Value;
                row["CreatedDate"] = DateTime.UtcNow;
                row["UpdatedDate"] = DateTime.UtcNow;
                row["SourceSystem"] = (object)device.SourceSystem ?? DBNull.Value;
                row["EventType"] = (object)device.EventType ?? DBNull.Value;
                row["CircleCode"] = (object)device.CircleCode ?? "GLOBAL";
                row["EventTime"] = device.EventTime;
                row["TenantId"] = (object)device.TenantId ?? DBNull.Value;
                row["Status"] = (object)device.Status ?? DBNull.Value;
                row["MetadataJson"] = (object)device.MetadataJson ?? DBNull.Value;

                dt.Rows.Add(row);
            }

            return dt;
        }

        #endregion
    }
}