using System.Globalization;
using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

public record DashboardStatsDto(int ActiveLearners, int CompletedEnrollments);

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    public DashboardController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Get dashboard statistics.
    /// Backoffice: all active users + all completed enrollments.
    /// Manager: team member count + team completions.
    /// Learner: 0 + own completed enrollments.
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        if (_user.IsBackOffice)
        {
            var activeLearners = await _db.Users
                .CountAsync(u => u.LockoutEnd == null || u.LockoutEnd < DateTimeOffset.UtcNow);

            var completed = await _db.Enrollments
                .CountAsync(e => e.Status == EnrollmentStatus.Completed);

            return new DashboardStatsDto(activeLearners, completed);
        }

        if (_user.Teams.Count > 0)
        {
            // Manager: show team stats
            var teamIdStrings = _user.Teams
                .Select(t => t.ToString(CultureInfo.InvariantCulture))
                .ToList();

            var memberIds = await _db.UserClaims
                .Where(c => c.ClaimType == "team" && teamIdStrings.Contains(c.ClaimValue!))
                .Select(c => c.UserId)
                .Distinct()
                .ToListAsync();

            var completed = await _db.Enrollments
                .CountAsync(e => memberIds.Contains(e.UserId) && e.Status == EnrollmentStatus.Completed);

            return new DashboardStatsDto(memberIds.Count, completed);
        }

        // Learner: own stats
        var ownCompleted = await _db.Enrollments
            .CountAsync(e => e.UserId == _user.UserId && e.Status == EnrollmentStatus.Completed);

        return new DashboardStatsDto(0, ownCompleted);
    }
}
