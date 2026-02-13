using ClearPulse.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClearPulse.Core.Services
{
    public interface IMetricsService
    {
        Task<MetricsDto> GetCorporateMetricsAsync(string? tenantId = null);
        Task<MetricsDto> GetCircleMetricsAsync(string circleCode, string? tenantId = null);
        Task<IEnumerable<MetricsDto>> GetHistoricalMetricsAsync(int days, string? tenantId = null);
    }

}
