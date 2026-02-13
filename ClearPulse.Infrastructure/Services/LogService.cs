using ClearPulse.Core.Entities;
using ClearPulse.Core.Services;
using ClearPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClearPulse.Infrastructure.Services
{
    public class LogService : ILogService
    {
        private readonly ClearPulseDbContext _db;
        public LogService(ClearPulseDbContext db) => _db = db;

        public async Task<IEnumerable<LogEvent>> GetLogsAsync(
            string sourceSystem, string? eventType, DateTime? from, DateTime? to,
            string? tenantId, string? circleCode, int take = 500)
        {
            var q = _db.LogEvents.AsQueryable().Where(l => l.SourceSystem == sourceSystem);

            if (!string.IsNullOrWhiteSpace(eventType)) q = q.Where(l => l.EventType == eventType);
            if (from.HasValue) q = q.Where(l => l.EventTime >= from.Value);
            if (to.HasValue) q = q.Where(l => l.EventTime <= to.Value);
            if (!string.IsNullOrEmpty(tenantId)) q = q.Where(l => l.TenantId == tenantId);
            if (!string.IsNullOrEmpty(circleCode)) q = q.Where(l => l.CircleCode == circleCode);

            return await q.OrderByDescending(l => l.EventTime).Take(take).ToListAsync();
        }
    }


}
