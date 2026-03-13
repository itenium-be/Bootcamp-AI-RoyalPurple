using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoadmapController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    public RoadmapController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Get the roadmap for the current user's competence centre.
    /// Default returns tier 1 and 2 skills (8-12 nodes).
    /// Pass showAll=true to return the full roadmap.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<SkillEntity>>> GetRoadmap([FromQuery] bool showAll = false)
    {
        var teamIds = _user.Teams.ToList();
        var query = _db.Skills
            .Include(s => s.Category)
            .Where(s => s.Category.TeamId != null && teamIds.Contains(s.Category.TeamId.Value));

        if (!showAll)
            query = query.Where(s => s.Tier <= 2);

        var skills = await query
            .OrderBy(s => s.Tier)
            .ThenBy(s => s.Name)
            .ToListAsync();

        return Ok(skills);
    }
}
