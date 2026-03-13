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
    /// Get enrollments. Backoffice sees all, others see their own.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<EnrollmentEntity>>> GetEnrollments()
    {
        var query = _db.Enrollments.Include(e => e.Course).AsQueryable();

        if (!_user.IsBackOffice)
        {
            query = query.Where(e => e.UserId == _user.UserId);
        }

        return await query.ToListAsync();
    }

    /// <summary>
    /// Enroll the current user in a course.
    /// </summary>
    [HttpPost("{courseId:int}")]
    public async Task<ActionResult<EnrollmentEntity>> Enroll(int courseId)
    {
        var course = await _db.Courses.FindAsync(courseId);
        if (course == null)
        {
            return NotFound();
        }

        var existing = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == _user.UserId && e.CourseId == courseId);
        if (existing != null)
        {
            return BadRequest("Already enrolled in this course.");
        }

        var enrollment = new EnrollmentEntity
        {
            UserId = _user.UserId!,
            CourseId = courseId,
        };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEnrollments), new { id = enrollment.Id }, enrollment);
    }

    /// <summary>
    /// Update the status of an enrollment (own only, unless backoffice).
    /// </summary>
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateEnrollmentStatusRequest request)
    {
        var enrollment = await _db.Enrollments.FindAsync(id);
        if (enrollment == null)
        {
            return NotFound();
        }

        if (!_user.IsBackOffice && enrollment.UserId != _user.UserId)
        {
            return Forbid();
        }

        enrollment.Status = request.Status;
        if (request.Status == EnrollmentStatus.Completed)
        {
            enrollment.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Unenroll from a course (own only, unless backoffice).
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Unenroll(int id)
    {
        var enrollment = await _db.Enrollments.FindAsync(id);
        if (enrollment == null)
        {
            return NotFound();
        }

        if (!_user.IsBackOffice && enrollment.UserId != _user.UserId)
        {
            return Forbid();
        }

        _db.Enrollments.Remove(enrollment);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
