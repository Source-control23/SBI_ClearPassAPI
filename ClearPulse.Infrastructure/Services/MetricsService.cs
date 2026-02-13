using ClearPulse.Core.DTOs;
using ClearPulse.Core.Services;
using ClearPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClearPulse.Infrastructure.Services
{
    public class MetricsService : IMetricsService
    {
        private readonly ClearPulseDbContext _db;
        public MetricsService(ClearPulseDbContext db) => _db = db;

        public async Task<MetricsDto> GetCorporateMetricsAsync(string? tenantId = null)
        {
            var dev = _db.DeviceAuthEvents.AsQueryable();
            var nas = _db.NasDevices.AsQueryable();
            if (!string.IsNullOrEmpty(tenantId))
            {
                dev = dev.Where(e => e.TenantId == tenantId);
                nas = nas.Where(e => e.TenantId == tenantId);
            }

            return new MetricsDto
            {
                Auth8021xCount = await dev.CountAsync(e => e.AuthType == "802.1x"),
                MabCount = await dev.CountAsync(e => e.AuthType == "MAB"),
                HealthyCount = await dev.CountAsync(e => e.Status == "Healthy"),
                UnhealthyCount = await dev.CountAsync(e => e.Status == "Unhealthy"),
                NasTotalCount = await nas.CountAsync(),
                NasActiveCount = await nas.CountAsync(n => n.RequestCount > 0),
                SnapshotTime = DateTime.UtcNow
            };
        }

        public async Task<MetricsDto> GetCircleMetricsAsync(string circleCode, string? tenantId = null)
        {
            var dev = _db.DeviceAuthEvents.Where(e => e.CircleCode == circleCode);
            var nas = _db.NasDevices.Where(e => e.CircleCode == circleCode);
            if (!string.IsNullOrEmpty(tenantId))
            {
                dev = dev.Where(e => e.TenantId == tenantId);
                nas = nas.Where(e => e.TenantId == tenantId);
            }

            return new MetricsDto
            {
                Auth8021xCount = await dev.CountAsync(e => e.AuthType == "802.1x"),
                MabCount = await dev.CountAsync(e => e.AuthType == "MAB"),
                HealthyCount = await dev.CountAsync(e => e.Status == "Healthy"),
                UnhealthyCount = await dev.CountAsync(e => e.Status == "Unhealthy"),
                NasTotalCount = await nas.CountAsync(),
                NasActiveCount = await nas.CountAsync(n => n.RequestCount > 0),
                SnapshotTime = DateTime.UtcNow
            };
        }

        public async Task<IEnumerable<MetricsDto>> GetHistoricalMetricsAsync(int days, string? tenantId = null)
        {
            var from = DateTime.UtcNow.Date.AddDays(-days);
            var dev = _db.DeviceAuthEvents.Where(e => e.EventTime >= from);
            if (!string.IsNullOrEmpty(tenantId)) dev = dev.Where(e => e.TenantId == tenantId);

            return await dev
                .GroupBy(e => ((DateTime)e.EventTime).Date)
                .Select(g => new MetricsDto
                {
                    Auth8021xCount = g.Count(e => e.AuthType == "802.1x"),
                    MabCount = g.Count(e => e.AuthType == "MAB"),
                    HealthyCount = g.Count(e => e.Status == "Healthy"),
                    UnhealthyCount = g.Count(e => e.Status == "Unhealthy"),
                    SnapshotTime = g.Key
                })
                .OrderBy(x => x.SnapshotTime)
                .ToListAsync();
        }
    }

}
