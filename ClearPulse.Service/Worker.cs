using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ClearPulse.Core.Entities;
using ClearPulse.Service.Parsers;
using ClearPulse.Service.Classifiers;
using ClearPulse.Service.Data;
using ClearPulse.Service.Inventory;

namespace ClearPulse.Service
{
    /// <summary>
    /// Main background worker service for processing ClearPass logs
    /// Optimized with separated concerns for better maintainability
    /// </summary>
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _defaultTenantId;
        private readonly LogParser _logParser;
        private readonly EventClassifier _eventClassifier;
        private readonly DatabaseWriter _databaseWriter;
        private readonly DeviceInventoryManager _inventoryManager;
        private readonly DeviceInventoryBuilder _inventoryBuilder;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _defaultTenantId = _configuration.GetValue<string>("LogProcessor:DefaultTenantId") ?? "SBI-TENANT-1";

            // Initialize service components
            _logParser = new LogParser();
            _eventClassifier = new EventClassifier(_logParser, _defaultTenantId);
            _databaseWriter = new DatabaseWriter(logger);
            _inventoryManager = new DeviceInventoryManager(logger);
            _inventoryBuilder = new DeviceInventoryBuilder();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var bufferPath = _configuration.GetValue<string>("LogProcessor:BufferPath") ?? "C:/logstash-buffer";
            var archivePath = _configuration.GetValue<string>("LogProcessor:ArchivePath") ?? Path.Combine(bufferPath, "archive");
            var connectionString = _configuration.GetValue<string>("LogProcessor:ConnectionString");

            Directory.CreateDirectory(bufferPath);
            Directory.CreateDirectory(archivePath);

            _logger.LogInformation("ClearPulse Worker started. Monitoring: {path}", bufferPath);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var files = Directory.GetFiles(bufferPath, "*.jsonl")
                        .OrderBy(File.GetLastWriteTimeUtc)
                        .ToArray();

                    foreach (var file in files)
                    {
                        if (stoppingToken.IsCancellationRequested)
                            break;

                        if (IsFileLocked(file))
                        {
                            _logger.LogWarning("File busy: {file}", Path.GetFileName(file));
                            continue;
                        }

                        await ProcessFileAsync(file, connectionString, stoppingToken);

                        // Archive processed file
                        string dest = Path.Combine(archivePath, $"{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(file)}");
                        File.Move(file, dest);
                        _logger.LogInformation("Archived: {file}", Path.GetFileName(file));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker execution error");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }

            _logger.LogInformation("ClearPulse Worker stopped");
        }

        private async Task ProcessFileAsync(string filePath, string connectionString, CancellationToken ct)
        {
            _logger.LogInformation("Processing file: {file}", Path.GetFileName(filePath));

            // Initialize batches for different entity types
            var batches = new Dictionary<string, List<BaseLogEntity>>(StringComparer.OrdinalIgnoreCase)
            {
                { "AdIntegrations", new List<BaseLogEntity>() },
                { "ClusterNodes", new List<BaseLogEntity>() },
                { "NasDevices", new List<BaseLogEntity>() },
                { "LogEvents", new List<BaseLogEntity>() },
                { "QuarantineDetails", new List<BaseLogEntity>() },
                { "DeviceAuthEvents", new List<BaseLogEntity>() }
            };

            var nasIpTracker = new HashSet<string>(); // Track unique NAS IPs
            int lineNumber = 0;
            int processedCount = 0;
            int errorCount = 0;

            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);

                string line;
                while ((line = await sr.ReadLineAsync()) != null)
                {
                    lineNumber++;

                    // Skip empty lines and heartbeat messages
                    if (string.IsNullOrWhiteSpace(line) ||
                        line.Contains("logstash-alive") ||
                        line.Contains("heartbeat"))
                        continue;

                    try
                    {
                        await ProcessLogLineAsync(line, batches, nasIpTracker);
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogWarning(ex, "Error processing line {lineNum} in {file}", lineNumber, Path.GetFileName(filePath));
                    }
                }

                _logger.LogInformation("Parsed {count} log entries from {file} ({errors} errors)",
                    processedCount, Path.GetFileName(filePath), errorCount);

                // Bulk insert all batches
                await BulkInsertAllBatchesAsync(batches, connectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file: {file}", filePath);
                throw;
            }
        }

        private async Task ProcessLogLineAsync(string line, Dictionary<string, List<BaseLogEntity>> batches, HashSet<string> nasIpTracker)
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            // Extract common fields
            string eventCode = _logParser.GetJsonString(root, "event", "code") ?? "";
            string alerts = _logParser.GetJsonString(root, "Common.Alerts") ?? "";
            string nasIp = _logParser.GetJsonString(root, "Common.NAS-IP-Address");
            string user = _logParser.GetJsonString(root, "Common.Username") ?? "";
            DateTime eventTime = _logParser.ParseTimestamp(root);

            var (hostname, circleCode, branchCode, domain) = _logParser.ParseUsername(user);

            bool typeFound = false;

            // Try to classify as AD Integration event
            if (_eventClassifier.TryClassifyAdIntegration(root, eventCode, alerts, eventTime, line, out var adIntegration))
            {
                batches["AdIntegrations"].Add(adIntegration);
                typeFound = true;
            }
            // Try to classify as Cluster Node event
            else if (_eventClassifier.TryClassifyClusterNode(root, eventCode, alerts, eventTime, line, out var clusterNode))
            {
                batches["ClusterNodes"].Add(clusterNode);
                typeFound = true;
            }
            // Try to classify as Device Authentication event
            else if (_eventClassifier.TryClassifyDeviceAuth(root, user, eventTime, line, out var deviceAuth, out var quarantine))
            {
                batches["DeviceAuthEvents"].Add(deviceAuth);

                if (quarantine != null)
                {
                    batches["QuarantineDetails"].Add(quarantine);
                }

                typeFound = true;
            }

            // Track NAS devices
            if (!string.IsNullOrEmpty(nasIp) && !nasIpTracker.Contains(nasIp))
            {
                var nasDevice = _eventClassifier.CreateNasDevice(nasIp, circleCode, eventTime, line);
                if (nasDevice != null)
                {
                    batches["NasDevices"].Add(nasDevice);
                    nasIpTracker.Add(nasIp);
                }
            }

            // If event doesn't match any specific type, log as generic event
            if (!typeFound)
            {
                var logEvent = _eventClassifier.CreateGenericLogEvent(eventTime, circleCode, line);
                batches["LogEvents"].Add(logEvent);
            }
        }

        private async Task BulkInsertAllBatchesAsync(Dictionary<string, List<BaseLogEntity>> batches, string connectionString)
        {
            // First, process all event batches (these are simple inserts)
            foreach (var kvp in batches)
            {
                if (kvp.Key == "DeviceAuthEvents")
                    continue; // Process separately for inventory

                if (kvp.Value.Count > 0)
                {
                    try
                    {
                        await _databaseWriter.BulkInsertAsync(kvp.Key, kvp.Value, connectionString);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to insert batch for table: {table}", kvp.Key);
                    }
                }
            }

            // Process DeviceAuthEvents - these go to both DeviceAuthEvents table AND DeviceInventory
            if (batches.ContainsKey("DeviceAuthEvents") && batches["DeviceAuthEvents"].Count > 0)
            {
                var authEvents = batches["DeviceAuthEvents"].Cast<DeviceAuthEvent>().ToList();

                try
                {
                    // Insert into DeviceAuthEvents table (historical record)
                    await _databaseWriter.BulkInsertAsync("DeviceAuthEvents", batches["DeviceAuthEvents"], connectionString);
                    _logger.LogInformation($"Inserted {authEvents.Count} device auth events");

                    // Build device inventory records from auth events
                    var inventoryRecords = _inventoryBuilder.BuildFromAuthEvents(authEvents);

                    if (inventoryRecords.Count > 0)
                    {
                        // UPSERT into DeviceInventory table (current state)
                        await _inventoryManager.UpsertDeviceInventoryAsync(inventoryRecords, connectionString);
                        _logger.LogInformation($"Upserted {inventoryRecords.Count} device inventory records");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process device auth events and inventory");
                }
            }
        }

        private bool IsFileLocked(string filePath)
        {
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }
    }
}