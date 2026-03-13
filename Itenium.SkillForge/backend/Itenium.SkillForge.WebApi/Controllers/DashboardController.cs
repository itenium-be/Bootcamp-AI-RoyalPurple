using Itenium.SkillForge.Data;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ISkillForgeUser _currentUser;
    private readonly AppDbContext _db;

    public DashboardController(IDashboardService dashboardService, ISkillForgeUser currentUser, AppDbContext db)
    {
        _dashboardService = dashboardService;
        _currentUser = currentUser;
        _db = db;
    }

    /// <summary>
    /// Get consultant summaries for all teams the current coach manages.
    /// Includes activity status, readiness flags (stub), and active goal count (stub).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "manager,backoffice")]
    public async Task<ActionResult<IList<ConsultantSummaryDto>>> GetDashboard()
    {
        var summaries = await _dashboardService.GetConsultantSummariesAsync(_currentUser.Teams);
        return Ok(summaries);
    }

    /// <summary>
    /// Get overview stats: total courses, active learners in manager's teams, assigned courses.
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "manager,backoffice")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        var teamIds = _currentUser.Teams;

        var totalCourses = await _db.Courses.CountAsync();

        var activeLearners = await _db.ConsultantProfiles
            .Where(p => teamIds.Contains(p.TeamId))
            .CountAsync();

        var assignedCourses = await _db.CourseAssignments
            .Where(a => a.TeamId != null && teamIds.Contains(a.TeamId.Value))
            .CountAsync();

        return Ok(new DashboardStatsDto(totalCourses, activeLearners, assignedCourses));
    }

    /// <summary>
    /// Record that the current user was active right now.
    /// </summary>
    [HttpPost("activity")]
    public async Task<IActionResult> RecordActivity()
    {
        await _dashboardService.RecordActivityAsync(_currentUser.UserId!);
        return NoContent();
    }
}

public record DashboardStatsDto(int TotalCourses, int ActiveLearners, int AssignedCourses);
