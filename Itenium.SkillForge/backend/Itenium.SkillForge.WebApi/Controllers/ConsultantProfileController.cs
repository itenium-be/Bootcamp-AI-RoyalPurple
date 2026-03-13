using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

public record ConsultantDto(string UserId, int? TeamId, string? TeamName);

public record AssignProfileRequest(int TeamId);

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConsultantProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    public ConsultantProfileController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Get all consultant profile assignments. Visible to coaches and backoffice.
    /// </summary>
    [HttpGet("consultants")]
    public async Task<ActionResult<List<ConsultantDto>>> GetConsultants()
    {
        var profiles = await _db.ConsultantProfiles
            .Include(p => p.Team)
            .Select(p => new ConsultantDto(p.UserId, p.TeamId, p.Team!.Name))
            .ToListAsync();

        return Ok(profiles);
    }

    /// <summary>
    /// Assign or update a consultant's competence centre profile.
    /// </summary>
    [HttpPut("consultants/{userId}")]
    public async Task<IActionResult> AssignProfile(string userId, [FromBody] AssignProfileRequest request)
    {
        var teamExists = await _db.Teams.AnyAsync(t => t.Id == request.TeamId);
        if (!teamExists)
        {
            return NotFound();
        }

        var existing = await _db.ConsultantProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (existing != null)
        {
            existing.TeamId = request.TeamId;
            existing.AssignedAt = DateTime.UtcNow;
        }
        else
        {
            _db.ConsultantProfiles.Add(new ConsultantProfileEntity
            {
                UserId = userId,
                TeamId = request.TeamId,
            });
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Remove a consultant's profile assignment.
    /// </summary>
    [HttpDelete("consultants/{userId}")]
    public async Task<IActionResult> RemoveProfile(string userId)
    {
        var profile = await _db.ConsultantProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            return NotFound();
        }

        _db.ConsultantProfiles.Remove(profile);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Get the current consultant's profile assignment.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<ConsultantDto>> GetMyProfile()
    {
        var userId = _user.UserId;
        var profile = await _db.ConsultantProfiles
            .Include(p => p.Team)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            return NotFound();
        }

        return Ok(new ConsultantDto(profile.UserId, profile.TeamId, profile.Team?.Name));
    }
}
