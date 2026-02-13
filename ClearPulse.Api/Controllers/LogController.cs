using ClearPulse.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClearPulse.Api.Controllers
{
    [ApiController]
    [Route("api/logs")]
    public class LogController : ControllerBase
    {
        private readonly ILogService _svc;
        public LogController(ILogService svc) => _svc = svc;

        [HttpGet("{sourceSystem}")]
        [Authorize]
        public async Task<IActionResult> Get(string sourceSystem, [FromQuery] string? eventType, [FromQuery] string? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int take = 500)
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            var circleCode = User.IsInRole("CircleHead") ? User.FindFirst("circleCode")?.Value : null;

            var logs = await _svc.GetLogsAsync(sourceSystem, eventType, from, to, tenantId, circleCode, take);
            if (!string.IsNullOrEmpty(status))
                logs = logs.Where(l => l.Status == status);

            return Ok(logs);
        }
    }

}
