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
public class TeamController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    public TeamController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Get the teams the current user has access to.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TeamEntity>>> GetUserTeams()
    {
        if (_user.IsBackOffice)
        {
            return await _db.Teams.ToListAsync();
        }

        return await _db.Teams
            .Where(t => _user.Teams.Contains(t.Id))
            .ToListAsync();
    }

    /// <summary>
    /// Create a new team. Backoffice only.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TeamEntity>> CreateTeam([FromBody] CreateTeamRequest request)
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var team = new TeamEntity { Name = request.Name };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUserTeams), new { }, team);
    }

    /// <summary>
    /// Update a team. Backoffice can update any team; managers can only update their own teams.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TeamEntity>> UpdateTeam(int id, [FromBody] UpdateTeamRequest request)
    {
        if (!_user.IsBackOffice && (!_user.IsManager || !_user.Teams.Contains(id)))
        {
            return Forbid();
        }

        var team = await _db.Teams.FindAsync(id);
        if (team == null)
        {
            return NotFound();
        }

        team.Name = request.Name;
        await _db.SaveChangesAsync();

        return Ok(team);
    }

    /// <summary>
    /// Delete a team. Backoffice only.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteTeam(int id)
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var team = await _db.Teams.FindAsync(id);
        if (team == null)
        {
            return NotFound();
        }

        _db.Teams.Remove(team);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
