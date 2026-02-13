using ClearPulse.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClearPulse.Core.Services
{
    public interface ILogService
    {
        Task<IEnumerable<LogEvent>> GetLogsAsync(
            string sourceSystem, string? eventType, DateTime? from, DateTime? to,
            string? tenantId, string? circleCode, int take = 500);
    }

}
