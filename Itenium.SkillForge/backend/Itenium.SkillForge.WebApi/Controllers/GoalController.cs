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
            .Include(g => g.ReadinessFlags)
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

    /// <summary>
    /// Consultant raises a readiness flag on their own goal, signalling they are
    /// ready for coach review. Maximum one active flag per goal at a time.
    /// </summary>
    [HttpPost("{goalId}/readiness-flag")]
    public async Task<IActionResult> RaiseReadinessFlag(int goalId)
    {
        var goal = await _db.Goals.FindAsync(goalId);
        if (goal == null)
            return NotFound();

        if (goal.ConsultantId != _user.UserId && !_user.IsManager && !_user.IsBackOffice)
            return Forbid();

        var alreadyActive = await _db.ReadinessFlags
            .AnyAsync(f => f.GoalId == goalId && f.ResolvedAt == null);

        if (alreadyActive)
            return Conflict();

        _db.ReadinessFlags.Add(new ReadinessFlagEntity
        {
            GoalId = goalId,
            RaisedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Resolves (dismisses) the active readiness flag on a goal.
    /// Consultant can resolve their own flag; manager can resolve any flag on their team's goals.
    /// </summary>
    [HttpDelete("{goalId}/readiness-flag")]
    public async Task<IActionResult> ResolveReadinessFlag(int goalId)
    {
        var goal = await _db.Goals.FindAsync(goalId);
        if (goal == null)
            return NotFound();

        var isOwner = goal.ConsultantId == _user.UserId;
        var isCoach = (_user.IsManager || _user.IsBackOffice) && goal.CoachId == _user.UserId;

        if (!isOwner && !isCoach)
            return Forbid();

        var flag = await _db.ReadinessFlags
            .FirstOrDefaultAsync(f => f.GoalId == goalId && f.ResolvedAt == null);

        if (flag == null)
            return NotFound();

        flag.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static GoalDto ToDto(GoalEntity g) =>
        new(
            g.Id,
            g.Skill.Name,
            g.CurrentLevel,
            g.TargetLevel,
            g.Deadline,
            g.Resources.Select(r => new GoalResourceDto(r.Id, r.Title, r.Url, r.Type)).ToList(),
            g.ReadinessFlags.Any(f => f.ResolvedAt == null));
}
