using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/readiness-flag")]
[Authorize]
public class ReadinessFlagController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    public ReadinessFlagController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Returns all active readiness flags (manager/backoffice only).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IList<ReadinessFlagDto>>> GetFlags()
    {
        if (!_user.IsManager && !_user.IsBackOffice)
            return Forbid();

        var flags = await _db.ReadinessFlags
            .Include(f => f.Goal).ThenInclude(g => g.Skill)
            .ToListAsync();

        var now = DateTime.UtcNow;
        return Ok(flags
            .Select(f => new ReadinessFlagDto(
                f.GoalId,
                f.Goal.Skill.Name,
                f.Goal.ConsultantId,
                f.RaisedAt,
                (int)(now - f.RaisedAt).TotalDays))
            .ToList());
    }

    /// <summary>
    /// Consultant raises a readiness flag on one of their active goals.
    /// Re-raising an existing flag resets the timestamp.
    /// </summary>
    [HttpPost("{goalId:int}")]
    public async Task<ActionResult> RaiseFlag(int goalId)
    {
        var goal = await _db.Goals.FindAsync(goalId);
        if (goal == null)
            return NotFound();

        if (goal.ConsultantId != _user.UserId && !_user.IsManager && !_user.IsBackOffice)
            return Forbid();

        var existing = await _db.ReadinessFlags.FindAsync(goalId);
        if (existing != null)
        {
            existing.RaisedAt = DateTime.UtcNow;
        }
        else
        {
            _db.ReadinessFlags.Add(new ReadinessFlagEntity { GoalId = goalId, RaisedAt = DateTime.UtcNow });
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Consultant lowers their readiness flag on a goal.
    /// </summary>
    [HttpDelete("{goalId:int}")]
    public async Task<ActionResult> LowerFlag(int goalId)
    {
        var flag = await _db.ReadinessFlags
            .Include(f => f.Goal)
            .FirstOrDefaultAsync(f => f.GoalId == goalId);

        if (flag == null)
            return NotFound();

        if (flag.Goal.ConsultantId != _user.UserId && !_user.IsManager && !_user.IsBackOffice)
            return Forbid();

        _db.ReadinessFlags.Remove(flag);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
