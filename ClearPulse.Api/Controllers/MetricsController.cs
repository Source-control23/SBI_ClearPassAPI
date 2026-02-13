using ClearPulse.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClearPulse.Api.Controllers
{
    [ApiController]
    [Route("api/metrics")]
    public class MetricsController : ControllerBase
    {
        private readonly IMetricsService _svc;
        public MetricsController(IMetricsService svc) => _svc = svc;

        [HttpGet("corporate")]
        [Authorize(Policy = "Corporate")]
        public async Task<IActionResult> Corporate()
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            return Ok(await _svc.GetCorporateMetricsAsync(tenantId));
        }

        [HttpGet("circle/{circleCode}")]
        [Authorize(Policy = "CircleHead")]
        public async Task<IActionResult> Circle(string circleCode)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var userCircle = User.FindFirst("circleCode")?.Value;
            if (!string.Equals(circleCode, userCircle, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            return Ok(await _svc.GetCircleMetricsAsync(circleCode, tenantId));
        }

        [HttpGet("historical/{days}")]
        [Authorize(Policy = "Corporate")]
        public async Task<IActionResult> Historical(int days = 7)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            return Ok(await _svc.GetHistoricalMetricsAsync(days, tenantId));
        }
    }

}
