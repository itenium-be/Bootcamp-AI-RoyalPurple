using System.Globalization;
using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public async Task<ActionResult<TeamEntity>> CreateTeam([FromBody] TeamRequest request)
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var team = new TeamEntity { Name = request.Name };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUserTeams), new { id = team.Id }, team);
    }

    /// <summary>
    /// Update a team. Backoffice only.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TeamEntity>> UpdateTeam(int id, [FromBody] TeamRequest request)
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

    /// <summary>
    /// Get members of a team. Backoffice or member of the team.
    /// </summary>
    [HttpGet("{id:int}/members")]
    public async Task<ActionResult<List<UserDto>>> GetMembers(int id)
    {
        if (!_user.IsBackOffice && !_user.Teams.Contains(id))
        {
            return Forbid();
        }

        var team = await _db.Teams.FindAsync(id);
        if (team == null)
        {
            return NotFound();
        }

        var teamIdStr = id.ToString(CultureInfo.InvariantCulture);
        var memberIds = await _db.UserClaims
            .Where(c => c.ClaimType == "team" && c.ClaimValue == teamIdStr)
            .Select(c => c.UserId)
            .ToListAsync();

        var users = await _db.Users
            .Where(u => memberIds.Contains(u.Id))
            .ToListAsync();

        var userRoles = await _db.UserRoles
            .Where(ur => memberIds.Contains(ur.UserId))
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync();

        return users.Select(u => new UserDto(
            u.Id,
            u.UserName!,
            u.Email!,
            u.FirstName ?? string.Empty,
            u.LastName ?? string.Empty,
            userRoles.Where(ur => ur.UserId == u.Id).Select(ur => ur.Name!).ToList(),
            u.LockoutEnd == null || u.LockoutEnd < DateTimeOffset.UtcNow,
            [id]
        )).ToList();
    }

    /// <summary>
    /// Add a user to a team. Backoffice only.
    /// </summary>
    [HttpPost("{id:int}/members/{userId}")]
    public async Task<ActionResult> AddMember(int id, string userId)
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

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var teamIdStr = id.ToString(CultureInfo.InvariantCulture);
        var alreadyMember = await _db.UserClaims
            .AnyAsync(c => c.UserId == userId && c.ClaimType == "team" && c.ClaimValue == teamIdStr);
        if (alreadyMember)
        {
            return BadRequest("User is already a member of this team.");
        }

        _db.UserClaims.Add(new IdentityUserClaim<string>
        {
            UserId = userId,
            ClaimType = "team",
            ClaimValue = teamIdStr,
        });
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Get enrollment progress for all members of a team. Backoffice or team member.
    /// </summary>
    [HttpGet("{id:int}/progress")]
    public async Task<ActionResult<List<TeamMemberProgressDto>>> GetTeamProgress(int id)
    {
        if (!_user.IsBackOffice && !_user.Teams.Contains(id))
        {
            return Forbid();
        }

        var team = await _db.Teams.FindAsync(id);
        if (team == null)
        {
            return NotFound();
        }

        var teamIdStr = id.ToString(CultureInfo.InvariantCulture);
        var memberIds = await _db.UserClaims
            .Where(c => c.ClaimType == "team" && c.ClaimValue == teamIdStr)
            .Select(c => c.UserId)
            .ToListAsync();

        var members = await _db.Users
            .Where(u => memberIds.Contains(u.Id))
            .ToListAsync();

        var enrollments = await _db.Enrollments
            .Include(e => e.Course)
            .Where(e => memberIds.Contains(e.UserId))
            .ToListAsync();

        return members.Select(m => new TeamMemberProgressDto(
            m.Id,
            $"{m.FirstName} {m.LastName}".Trim(),
            m.Email!,
            enrollments
                .Where(e => e.UserId == m.Id)
                .Select(e => new EnrollmentProgressDto(
                    e.CourseId,
                    e.Course.Name,
                    e.Status,
                    e.EnrolledAt,
                    e.CompletedAt))
                .ToList()
        )).ToList();
    }

    /// <summary>
    /// Get courses assigned to a team. Backoffice or member of the team.
    /// </summary>
    [HttpGet("{id:int}/assignments")]
    public async Task<ActionResult<List<TeamAssignmentDto>>> GetTeamAssignments(int id)
    {
        if (!_user.IsBackOffice && !_user.Teams.Contains(id))
        {
            return Forbid();
        }

        var team = await _db.Teams.FindAsync(id);
        if (team == null)
        {
            return NotFound();
        }

        var assignments = await _db.TeamAssignments
            .Include(a => a.Course)
            .Where(a => a.TeamId == id)
            .ToListAsync();

        var memberUserIds = assignments
            .Where(a => a.UserId != null)
            .Select(a => a.UserId!)
            .ToHashSet(StringComparer.Ordinal);

        var memberUsers = memberUserIds.Count > 0
            ? await _db.Users.Where(u => memberUserIds.Contains(u.Id)).ToListAsync()
            : [];

        return assignments
            .Select(a =>
            {
                var member = a.UserId != null ? memberUsers.FirstOrDefault(u => u.Id == a.UserId) : null;
                var fullName = member != null ? $"{member.FirstName} {member.LastName}".Trim() : null;
                return new TeamAssignmentDto(a.CourseId, a.Course.Name, a.IsMandatory, a.AssignedAt, a.UserId, fullName);
            })
            .ToList();
    }

    /// <summary>
    /// Assign a course to a team. Backoffice or team manager.
    /// </summary>
    [HttpPost("{id:int}/assignments/{courseId:int}")]
    public async Task<ActionResult> AssignCourse(int id, int courseId, [FromBody] AssignCourseRequest request)
    {
        if (!_user.IsBackOffice && !_user.Teams.Contains(id))
        {
            return Forbid();
        }

        var team = await _db.Teams.FindAsync(id);
        if (team == null)
        {
            return NotFound();
        }

        var course = await _db.Courses.FindAsync(courseId);
        if (course == null)
        {
            return NotFound();
        }

        var alreadyAssigned = await _db.TeamAssignments
            .AnyAsync(a => a.TeamId == id && a.CourseId == courseId && a.UserId == request.UserId);
        if (alreadyAssigned)
        {
            return BadRequest("Course is already assigned.");
        }

        _db.TeamAssignments.Add(new TeamAssignmentEntity
        {
            TeamId = id,
            CourseId = courseId,
            IsMandatory = request.IsMandatory,
            UserId = request.UserId,
        });
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Unassign a course from a team. Backoffice or team manager.
    /// </summary>
    [HttpDelete("{id:int}/assignments/{courseId:int}")]
    public async Task<ActionResult> UnassignCourse(int id, int courseId, [FromQuery] string? userId = null)
    {
        if (!_user.IsBackOffice && !_user.Teams.Contains(id))
        {
            return Forbid();
        }

        var assignment = await _db.TeamAssignments
            .FirstOrDefaultAsync(a => a.TeamId == id && a.CourseId == courseId && a.UserId == userId);
        if (assignment == null)
        {
            return NotFound();
        }

        _db.TeamAssignments.Remove(assignment);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Update whether an assigned course is mandatory or optional. Backoffice or team manager.
    /// </summary>
    [HttpPut("{id:int}/assignments/{courseId:int}")]
    public async Task<ActionResult> UpdateAssignment(int id, int courseId, [FromBody] AssignCourseRequest request)
    {
        if (!_user.IsBackOffice && !_user.Teams.Contains(id))
        {
            return Forbid();
        }

        var assignment = await _db.TeamAssignments
            .FirstOrDefaultAsync(a => a.TeamId == id && a.CourseId == courseId && a.UserId == request.UserId);
        if (assignment == null)
        {
            return NotFound();
        }

        assignment.IsMandatory = request.IsMandatory;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Remove a user from a team. Backoffice only.
    /// </summary>
    [HttpDelete("{id:int}/members/{userId}")]
    public async Task<ActionResult> RemoveMember(int id, string userId)
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

        var teamIdStr = id.ToString(CultureInfo.InvariantCulture);
        var claim = await _db.UserClaims
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ClaimType == "team" && c.ClaimValue == teamIdStr);
        if (claim == null)
        {
            return NotFound();
        }

        _db.UserClaims.Remove(claim);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
