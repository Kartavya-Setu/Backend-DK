using DirectKarigar.Domain.Entities;
using DirectKarigar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DirectKarigar.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProfessionalsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProfessionalsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProfessionalProfile>>> GetProfessionals()
    {
        var pros = await _context.ProfessionalProfiles
            .Include(p => p.Reviews)
            .AsNoTracking()
            .ToListAsync();

        return Ok(pros);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProfessionalProfile>> GetProfessional(string id)
    {
        var pro = await _context.ProfessionalProfiles
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pro == null) return NotFound();
        return Ok(pro);
    }

    [HttpGet("nearby")]
    public async Task<ActionResult<List<ProfessionalProfile>>> GetNearby(
        [FromQuery] double lat = 12.9784,
        [FromQuery] double lng = 77.6408,
        [FromQuery] double radiusKm = 5.0,
        [FromQuery] string? trade = null)
    {
        var query = _context.ProfessionalProfiles.Include(p => p.Reviews).AsQueryable();

        if (!string.IsNullOrEmpty(trade))
        {
            query = query.Where(p => p.Trade.ToLower().Contains(trade.ToLower()));
        }

        var all = await query.ToListAsync();

        // Calculate Euclidean approximation for distance filtering
        var nearby = all
            .Where(p =>
            {
                var dLat = (p.Lat - lat) * 111.0;
                var dLng = (p.Lng - lng) * 111.0 * Math.Cos(lat * Math.PI / 180.0);
                var dist = Math.Sqrt(dLat * dLat + dLng * dLng);
                return dist <= radiusKm;
            })
            .OrderBy(p => p.PenaltyPoints)
            .ThenByDescending(p => p.PublicRating)
            .ToList();

        return Ok(nearby);
    }
}
