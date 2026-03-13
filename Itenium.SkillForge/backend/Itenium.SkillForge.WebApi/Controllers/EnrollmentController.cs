using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

public record EnrollmentDto(int Id, int CourseId, string CourseName, DateTime EnrolledAt);

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrollmentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    public EnrollmentController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Get all courses the current user is enrolled in.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<EnrollmentDto>>> GetMyEnrollments()
    {
        var enrollments = await _db.CourseEnrollments
            .Where(e => e.UserId == _user.UserId)
            .Include(e => e.Course)
            .Select(e => new EnrollmentDto(e.Id, e.CourseId, e.Course!.Name, e.EnrolledAt))
            .ToListAsync();

        return Ok(enrollments);
    }

    /// <summary>
    /// Enroll the current user in a course.
    /// </summary>
    [HttpPost("{courseId:int}")]
    public async Task<IActionResult> Enroll(int courseId)
    {
        var course = await _db.Courses.FindAsync(courseId);
        if (course == null)
        {
            return NotFound();
        }

        var existing = await _db.CourseEnrollments
            .FirstOrDefaultAsync(e => e.UserId == _user.UserId && e.CourseId == courseId);
        if (existing != null)
        {
            return Conflict();
        }

        var enrollment = new CourseEnrollmentEntity
        {
            UserId = _user.UserId!,
            CourseId = courseId,
        };

        _db.CourseEnrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        return Created($"/api/enrollment", enrollment.Id);
    }

    /// <summary>
    /// Unenroll the current user from a course.
    /// </summary>
    [HttpDelete("{courseId:int}")]
    public async Task<IActionResult> Unenroll(int courseId)
    {
        var enrollment = await _db.CourseEnrollments
            .FirstOrDefaultAsync(e => e.UserId == _user.UserId && e.CourseId == courseId);

        if (enrollment == null)
        {
            return NotFound();
        }

        _db.CourseEnrollments.Remove(enrollment);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
