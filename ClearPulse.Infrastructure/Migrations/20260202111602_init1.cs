using System;
using System.Data;
using System.Threading;
using Microsoft.Data.SqlClient;

namespace ClearPulse.Service
{
    public class BulkInsertOptions
    {
        // Number of rows per batch sent to server (SqlBulkCopy.BatchSize)
        public int? BatchSize { get; set; }

        // Timeout for each bulk copy operation in seconds
        public int BulkCopyTimeoutSeconds { get; set; } = 600;

        // Use table lock during bulk copy to improve throughput on large loads
        public bool UseTableLock { get; set; } = true;

        // Number of times to retry on transient failure
        public int Retries { get; set; } = 3;

        // Delay in seconds between retries (exponential backoff applied)
        public int RetryDelaySeconds { get; set; } = 2;
    }

    public static class BulkInserter
    {
        public static void InsertBatch(string connectionString, string tableName, DataTable table, BulkInsertOptions options = null)
        {
            options ??= new BulkInsertOptions();

            var attempt = 0;
            var lastEx = default(Exception);

            // Use defaults if caller didn't specify
            var batchSize = options.BatchSize;
            var timeout = options.BulkCopyTimeoutSeconds;
            var useTableLock = options.UseTableLock;
            var retries = Math.Max(1, options.Retries);
            var retryDelaySeconds = Math.Max(1, options.RetryDelaySeconds);

            while (++attempt <= retries)
            {
                try
                {
                    var copyOptions = useTableLock ? SqlBulkCopyOptions.TableLock : SqlBulkCopyOptions.Default;

                    using var conn = new SqlConnection(connectionString);
                    conn.Open();

                    using var bulkCopy = new SqlBulkCopy(conn, copyOptions, null)
                    {
                        DestinationTableName = tableName,
                        BulkCopyTimeout = timeout
                    };

                    if (batchSize.HasValue && batchSize.Value > 0)
                        bulkCopy.BatchSize = batchSize.Value;

                    // Important: map DataTable column names to destination table column names explicitly.
                    // This avoids accidental ordinal mapping mismatches between DataTable and database table schema.
                    foreach (DataColumn col in table.Columns)
                    {
                        // Only add mapping if column name is non-empty
                        if (!string.IsNullOrWhiteSpace(col.ColumnName))
                            bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    }

                    // Column mappings added — do the bulk write
                    bulkCopy.WriteToServer(table);

                    // Success - exit
                    return;
                }
                catch (Exception ex)
                {
                    lastEx = ex;

                    // If last attempt, rethrow with context
                    if (attempt >= retries)
                        throw new Exception($"Bulk insert to {tableName} failed after {attempt} attempt(s). See inner exception.", ex);

                    // Simple exponential backoff before retry
                    try
                    {
                        var delayMs = (int)(retryDelaySeconds * 1000 * Math.Pow(2, attempt - 1));
                        Thread.Sleep(delayMs);
                    }
                    catch (ThreadInterruptedException)
                    {
                        // ignore and retry immediately if interrupted
                    }
                }
            }

            // Should not reach here; defensive throw
            throw lastEx ?? new InvalidOperationException("Unknown failure in BulkInserter.InsertBatch");
        }
    }
}
