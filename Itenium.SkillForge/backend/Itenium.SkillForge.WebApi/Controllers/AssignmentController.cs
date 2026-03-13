using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/assignment")]
[Authorize(Roles = "manager,backoffice")]
public class AssignmentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _currentUser;

    public AssignmentController(AppDbContext db, ISkillForgeUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Get all assignments for the teams managed by the current coach. Admins see all.</summary>
    [HttpGet]
    public async Task<ActionResult<IList<AssignmentDto>>> GetAssignments()
    {
        var query = _db.CourseAssignments.Where(a => a.TeamId != null);

        if (!_currentUser.IsBackOffice)
        {
            var teamIds = _currentUser.Teams;
            query = query.Where(a => teamIds.Contains(a.TeamId!.Value));
        }

        var assignments = await query
            .Include(a => a.Course)
            .Select(a => new AssignmentDto(
                a.Id, a.CourseId, a.Course.Name, a.TeamId, a.UserId, a.IsRequired, a.AssignedAt))
            .ToListAsync();

        return Ok(assignments);
    }

    /// <summary>Assign a course to a team or individual member.</summary>
    [HttpPost]
    public async Task<ActionResult<AssignmentDto>> AssignCourse([FromBody] AssignCourseRequest request)
    {
        if (!_currentUser.IsBackOffice && !_currentUser.Teams.Contains(request.TeamId))
            return Forbid();

        var courseExists = await _db.Courses.AnyAsync(c => c.Id == request.CourseId);
        if (!courseExists)
            return NotFound();

        var assignment = new CourseAssignmentEntity
        {
            CourseId = request.CourseId,
            TeamId = request.TeamId,
            UserId = request.UserId,
            IsRequired = request.IsRequired,
            AssignedById = _currentUser.UserId!,
        };

        _db.CourseAssignments.Add(assignment);
        await _db.SaveChangesAsync();

        await _db.Entry(assignment).Reference(a => a.Course).LoadAsync();

        var dto = new AssignmentDto(
            assignment.Id, assignment.CourseId, assignment.Course.Name,
            assignment.TeamId, assignment.UserId, assignment.IsRequired, assignment.AssignedAt);

        return CreatedAtAction(nameof(GetAssignments), new { id = assignment.Id }, dto);
    }

    /// <summary>Remove a course assignment.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> RemoveAssignment(int id)
    {
        var assignment = await _db.CourseAssignments.FindAsync(id);
        if (assignment == null)
            return NotFound();

        if (!_currentUser.IsBackOffice && (assignment.TeamId == null || !_currentUser.Teams.Contains(assignment.TeamId.Value)))
            return Forbid();

        _db.CourseAssignments.Remove(assignment);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public record AssignmentDto(
    int Id,
    int CourseId,
    string CourseName,
    int? TeamId,
    string? UserId,
    bool IsRequired,
    DateTime AssignedAt);

public record AssignCourseRequest(
    int CourseId,
    int TeamId,
    string? UserId,
    bool IsRequired);
