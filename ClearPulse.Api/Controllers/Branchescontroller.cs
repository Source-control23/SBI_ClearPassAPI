using ClearPulse.Core.Entities;
using ClearPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClearPulse.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BranchesController : ControllerBase
    {
        private readonly ClearPulseDbContext _db;

        public BranchesController(ClearPulseDbContext db) => _db = db;

        /// <summary>
        /// Get all branches
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string circleCode = null)
        {
            var query = _db.BranchMaster.AsQueryable();

            if (!string.IsNullOrEmpty(circleCode))
                query = query.Where(b => b.CircleCode == circleCode);

            var branches = await query
                .OrderBy(b => b.CircleCode)
                .ThenBy(b => b.BranchCode)
                .ToListAsync();

            return Ok(branches);
        }

        /// <summary>
        /// Get branch by code
        /// </summary>
        [HttpGet("{branchCode}")]
        public async Task<IActionResult> GetByCode(string branchCode)
        {
            var branch = await _db.BranchMaster
                .FirstOrDefaultAsync(b => b.BranchCode == branchCode);

            if (branch == null)
                return NotFound();

            return Ok(branch);
        }

        /// <summary>
        /// Create new branch
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "Corporate")]
        public async Task<IActionResult> Create([FromBody] BranchMasterDto dto)
        {
            if (await _db.BranchMaster.AnyAsync(b => b.BranchCode == dto.BranchCode))
                return BadRequest("Branch code already exists");

            var branch = new Branch
            {
                BranchCode = dto.BranchCode,
                BranchName = dto.BranchName,
                CircleCode = dto.CircleCode,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                IsActive = dto.IsActive
            };

            _db.BranchMaster.Add(branch);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetByCode), new { branchCode = branch.BranchCode }, branch);
        }

        /// <summary>
        /// Update branch
        /// </summary>
        [HttpPut("{branchCode}")]
        [Authorize(Policy = "Corporate")]
        public async Task<IActionResult> Update(string branchCode, [FromBody] BranchMasterDto dto)
        {
            var branch = await _db.BranchMaster
                .FirstOrDefaultAsync(b => b.BranchCode == branchCode);

            if (branch == null)
                return NotFound();

            branch.BranchName = dto.BranchName;
            branch.CircleCode = dto.CircleCode;
            branch.Address = dto.Address;
            branch.City = dto.City;
            branch.State = dto.State;
            branch.IsActive = dto.IsActive;

            await _db.SaveChangesAsync();

            return Ok(branch);
        }

        /// <summary>
        /// Delete branch
        /// </summary>
        [HttpDelete("{branchCode}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(string branchCode)
        {
            var branch = await _db.BranchMaster
                .FirstOrDefaultAsync(b => b.BranchCode == branchCode);

            if (branch == null)
                return NotFound();

            _db.BranchMaster.Remove(branch);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Get branch statistics
        /// </summary>
        [HttpGet("{branchCode}/stats")]
        public async Task<IActionResult> GetStats(string branchCode)
        {
            var branch = await _db.BranchMaster
                .FirstOrDefaultAsync(b => b.BranchCode == branchCode);

            if (branch == null)
                return NotFound();

            // Get device count for this branch
            var deviceCount = await _db.DeviceInventory
                .Where(d => d.BranchCode == branchCode)
                .CountAsync();

            var healthyCount = await _db.DeviceInventory
                .Where(d => d.BranchCode == branchCode && d.CurrentHealthStatus == "Healthy")
                .CountAsync();

            return Ok(new
            {
                Branch = branch,
                Statistics = new
                {
                    TotalDevices = deviceCount,
                    HealthyDevices = healthyCount,
                    UnhealthyDevices = deviceCount - healthyCount,
                    HealthPercentage = deviceCount > 0 ? (healthyCount * 100.0 / deviceCount) : 0
                }
            });
        }
    }

    public class BranchMasterDto
    {
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string CircleCode { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public bool IsActive { get; set; } = true;
    }
}