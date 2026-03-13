using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ISkillForgeUser _currentUser;

    public DashboardController(IDashboardService dashboardService, ISkillForgeUser currentUser)
    {
        _dashboardService = dashboardService;
        _currentUser = currentUser;
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
    /// Record that the current user was active right now.
    /// </summary>
    [HttpPost("activity")]
    public async Task<IActionResult> RecordActivity()
    {
        await _dashboardService.RecordActivityAsync(_currentUser.UserId!);
        return NoContent();
    }
}
