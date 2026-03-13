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
public class GoalController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    public GoalController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Get active goals for the current user (learner: own goals; manager: specify consultantId).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IList<GoalDto>>> GetGoals([FromQuery] string? consultantId = null)
    {
        var targetId = _user.IsManager || _user.IsBackOffice
            ? (consultantId ?? _user.UserId!)
            : _user.UserId!;

        var goals = await _db.Goals
            .Include(g => g.Skill)
            .Include(g => g.Resources)
            .Where(g => g.ConsultantId == targetId)
            .OrderBy(g => g.Deadline)
            .ToListAsync();

        return Ok(goals.Select(ToDto).ToList());
    }

    /// <summary>
    /// Coach assigns a goal to a consultant.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<GoalDto>> CreateGoal([FromBody] CreateGoalRequest request)
    {
        if (!_user.IsManager && !_user.IsBackOffice)
            return Forbid();

        var goal = new GoalEntity
        {
            ConsultantId = request.ConsultantId,
            CoachId = _user.UserId!,
            SkillId = request.SkillId,
            CurrentLevel = request.CurrentLevel,
            TargetLevel = request.TargetLevel,
            Deadline = request.Deadline,
            Resources = request.Resources
                .Select(r => new GoalResourceEntity { Title = r.Title, Url = r.Url, Type = r.Type })
                .ToList(),
        };

        _db.Goals.Add(goal);
        await _db.SaveChangesAsync();

        await _db.Entry(goal).Reference(g => g.Skill).LoadAsync();

        return CreatedAtAction(nameof(GetGoals), new { }, ToDto(goal));
    }

    private static GoalDto ToDto(GoalEntity g) =>
        new(
            g.Id,
            g.Skill.Name,
            g.CurrentLevel,
            g.TargetLevel,
            g.Deadline,
            g.Resources.Select(r => new GoalResourceDto(r.Id, r.Title, r.Url, r.Type)).ToList());
}
