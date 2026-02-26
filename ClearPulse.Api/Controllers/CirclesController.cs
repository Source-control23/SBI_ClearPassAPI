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
    public class CirclesController : ControllerBase
    {
        private readonly ClearPulseDbContext _db;

        public CirclesController(ClearPulseDbContext db) => _db = db;

        /// <summary>
        /// Get all circles
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1,[FromQuery] int pageSize = 10)
        {
            var query = _db.CircleMaster
                .OrderBy(c => c.CircleCode)
                .AsQueryable();

            var totalRecords = await query.CountAsync();

            var circles = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = circles
            });
        }

        /// <summary>
        /// Get circle by code
        /// </summary>
        [HttpGet("{circleCode}")]
        public async Task<IActionResult> GetByCode(string circleCode)
        {
            var circle = await _db.CircleMaster
                .FirstOrDefaultAsync(c => c.CircleCode == circleCode);

            if (circle == null)
                return NotFound();

            return Ok(circle);
        }

        /// <summary>
        /// Create new circle
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create([FromBody] CircleMasterDto dto)
        {
            if (await _db.CircleMaster.AnyAsync(c => c.CircleCode == dto.CircleCode))
                return BadRequest("Circle code already exists");

            var circle = new Circle
            {
                CircleCode = dto.CircleCode,
                CircleName = dto.CircleName,
                State = dto.State,
                Region = dto.Region,
                IsActive = dto.IsActive,
   
            };

            _db.CircleMaster.Add(circle);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetByCode), new { circleCode = circle.CircleCode }, circle);
        }

        /// <summary>
        /// Update circle
        /// </summary>
        [HttpPut("{circleCode}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(string circleCode, [FromBody] CircleMasterDto dto)
        {
            var circle = await _db.CircleMaster
                .FirstOrDefaultAsync(c => c.CircleCode == circleCode);

            if (circle == null)
                return NotFound();

            circle.CircleName = dto.CircleName;
            circle.Region = dto.Region;
            circle.State = dto.State;
            circle.IsActive = dto.IsActive;
            await _db.SaveChangesAsync();

            return Ok(circle);
        }

        /// <summary>
        /// Delete circle
        /// </summary>
        [HttpDelete("{circleCode}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(string circleCode)
        {
            var circle = await _db.CircleMaster
                .FirstOrDefaultAsync(c => c.CircleCode == circleCode);

            if (circle == null)
                return NotFound();

            _db.CircleMaster.Remove(circle);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Get branches in a circle
        /// </summary>
        [HttpGet("{circleCode}/branches")]
        public async Task<IActionResult> GetBranches(string circleCode)
        {
            var branches = await _db.BranchMaster
                .Where(b => b.CircleCode == circleCode)
                .OrderBy(b => b.BranchCode)
                .ToListAsync();

            return Ok(branches);
        }
    }

    public class CircleMasterDto
    {
        public string CircleCode { get; set; }
        public string CircleName { get; set; }
        public string Region { get; set; }
        public string State { get; set; }
        public bool IsActive { get; set; } = true;
    }
}