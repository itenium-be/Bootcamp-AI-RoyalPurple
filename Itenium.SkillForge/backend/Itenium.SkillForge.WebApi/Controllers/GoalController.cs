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
    /// Get goals. Consultants see their own; coaches see all (FR17, FR20).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<GoalEntity>>> GetGoals()
    {
        if (_user.IsBackOffice || _user.IsManager)
        {
            return Ok(await _db.Goals.ToListAsync());
        }

        return Ok(await _db.Goals
            .Where(g => g.ConsultantUserId == _user.UserId)
            .ToListAsync());
    }

    /// <summary>
    /// Get a single goal by ID (FR17).
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<GoalEntity>> GetGoal(int id)
    {
        var goal = await _db.Goals.FindAsync(id);
        if (goal == null)
        {
            return NotFound();
        }

        if (!_user.IsBackOffice && !_user.IsManager && goal.ConsultantUserId != _user.UserId)
        {
            return Forbid();
        }

        return Ok(goal);
    }

    /// <summary>
    /// Coach assigns a goal to a consultant (FR16).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<GoalEntity>> CreateGoal([FromBody] CreateGoalRequest request)
    {
        if (!_user.IsManager)
        {
            return Forbid();
        }

        var goal = new GoalEntity
        {
            ConsultantUserId = request.ConsultantUserId,
            SkillName = request.SkillName,
            CurrentNiveau = request.CurrentNiveau,
            TargetNiveau = request.TargetNiveau,
            Deadline = request.Deadline,
            LinkedResources = request.LinkedResources,
            CreatedByCoachId = _user.UserId ?? string.Empty,
        };

        _db.Goals.Add(goal);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetGoal), new { id = goal.Id }, goal);
    }

    /// <summary>
    /// Coach updates a goal (FR16).
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<GoalEntity>> UpdateGoal(int id, [FromBody] UpdateGoalRequest request)
    {
        if (!_user.IsManager)
        {
            return Forbid();
        }

        var goal = await _db.Goals.FindAsync(id);
        if (goal == null)
        {
            return NotFound();
        }

        goal.SkillName = request.SkillName;
        goal.CurrentNiveau = request.CurrentNiveau;
        goal.TargetNiveau = request.TargetNiveau;
        goal.Deadline = request.Deadline;
        goal.LinkedResources = request.LinkedResources;

        await _db.SaveChangesAsync();

        return Ok(goal);
    }

    /// <summary>
    /// Coach deletes a goal.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteGoal(int id)
    {
        if (!_user.IsManager)
        {
            return Forbid();
        }

        var goal = await _db.Goals.FindAsync(id);
        if (goal == null)
        {
            return NotFound();
        }

        _db.Goals.Remove(goal);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Consultant raises a readiness flag on their goal (FR18).
    /// </summary>
    [HttpPost("{id:int}/readiness")]
    public async Task<ActionResult<GoalEntity>> RaiseReadinessFlag(int id)
    {
        var goal = await _db.Goals.FindAsync(id);
        if (goal == null)
        {
            return NotFound();
        }

        if (goal.ConsultantUserId != _user.UserId)
        {
            return Forbid();
        }

        if (goal.ReadinessFlagRaisedAt != null)
        {
            return BadRequest("A readiness flag is already raised for this goal.");
        }

        goal.ReadinessFlagRaisedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(goal);
    }

    /// <summary>
    /// Coach clears a readiness flag (FR20).
    /// </summary>
    [HttpDelete("{id:int}/readiness")]
    public async Task<ActionResult<GoalEntity>> ClearReadinessFlag(int id)
    {
        if (!_user.IsManager)
        {
            return Forbid();
        }

        var goal = await _db.Goals.FindAsync(id);
        if (goal == null)
        {
            return NotFound();
        }

        goal.ReadinessFlagRaisedAt = null;
        await _db.SaveChangesAsync();

        return Ok(goal);
    }
}

public record CreateGoalRequest(string ConsultantUserId, string SkillName, int CurrentNiveau, int TargetNiveau, DateTime Deadline, string? LinkedResources = null);

public record UpdateGoalRequest(string SkillName, int CurrentNiveau, int TargetNiveau, DateTime Deadline, string? LinkedResources = null);
