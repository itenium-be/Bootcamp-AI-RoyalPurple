using Itenium.SkillForge.Data;
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
    public async Task<ActionResult<List<SkillDto>>> GetRoadmap([FromQuery] bool showAll = false)
    {
        var teams = _user.Teams;
        var query = _db.Skills
            .Include(s => s.Category)
            .Include(s => s.Prerequisites)
                .ThenInclude(p => p.PrerequisiteSkill)
            .Where(s => s.Category.TeamId != null && teams.Contains(s.Category.TeamId.Value));

        if (!showAll)
            query = query.Where(s => s.Tier <= 2);

        var skills = await query
            .OrderBy(s => s.Tier)
            .ThenBy(s => s.Name)
            .ToListAsync();

        var dtos = skills
            .Select(s => new SkillDto(
                s.Id,
                s.Name,
                s.Description,
                s.Tier,
                s.Category.TeamId ?? 0,
                s.Prerequisites.Select(p => p.PrerequisiteSkill.Name).ToList()))
            .ToList();

        return Ok(dtos);
    }
}
