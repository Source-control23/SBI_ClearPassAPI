using System;
using System.Data;
using System.Threading;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace ClearPulse.Service
{
    public class BulkInsertOptions
    {
        public int? BatchSize { get; set; }
        public int BulkCopyTimeoutSeconds { get; set; } = 600;
        public bool UseTableLock { get; set; } = true;
        public int Retries { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 2;
    }

    public static class BulkInserter
    {
        public static void InsertBatch(string connectionString, string tableName, DataTable dataTable)
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
                return;

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            using var bulkCopy = new SqlBulkCopy(connection)
            {
                DestinationTableName = tableName,
                BatchSize = 1000,
                BulkCopyTimeout = 300
            };

            // Map columns
            foreach (DataColumn column in dataTable.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }

            try
            {
                bulkCopy.WriteToServer(dataTable);
            }
            catch (Exception ex)
            {
                throw new Exception($"Bulk insert failed for table {tableName}: {ex.Message}", ex);
            }
        }
    }
}
