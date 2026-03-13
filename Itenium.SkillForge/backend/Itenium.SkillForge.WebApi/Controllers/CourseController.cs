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
public class CourseController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _currentUser;

    public CourseController(AppDbContext db, ISkillForgeUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get all courses. Learners only see Published courses. Managers/backoffice see all.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CourseEntity>>> GetCourses(
        [FromQuery] int? teamId = null,
        [FromQuery] CourseStatus? status = null)
    {
        var query = _db.Courses.AsQueryable();

        if (teamId.HasValue)
            query = query.Where(c => c.TeamId == teamId.Value);

        if (!_currentUser.IsManager && !_currentUser.IsBackOffice)
            query = query.Where(c => c.Status == CourseStatus.Published);
        else if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return Ok(await query.ToListAsync());
    }

    /// <summary>
    /// Get a course by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CourseEntity>> GetCourse(int id)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course == null)
            return NotFound();

        return Ok(course);
    }

    /// <summary>
    /// Create a new course (defaults to Draft).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "backoffice,manager")]
    public async Task<ActionResult<CourseEntity>> CreateCourse([FromBody] CreateCourseRequest request)
    {
        var course = new CourseEntity
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Level = request.Level,
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
    }

    /// <summary>
    /// Update an existing course.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "backoffice,manager")]
    public async Task<ActionResult<CourseEntity>> UpdateCourse(int id, [FromBody] UpdateCourseRequest request)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course == null)
            return NotFound();

        course.Name = request.Name;
        course.Description = request.Description;
        course.Category = request.Category;
        course.Level = request.Level;

        await _db.SaveChangesAsync();

        return Ok(course);
    }

    /// <summary>
    /// Set the publish status of a course (Draft / Published / Archived).
    /// </summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "manager,backoffice")]
    public async Task<ActionResult<CourseEntity>> SetStatus(int id, [FromBody] SetCourseStatusRequest request)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course == null)
            return NotFound();

        course.Status = request.Status;
        await _db.SaveChangesAsync();

        return Ok(course);
    }

    /// <summary>
    /// Delete a course.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "backoffice")]
    public async Task<ActionResult> DeleteCourse(int id)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course == null)
            return NotFound();

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateCourseRequest(string Name, string? Description, string? Category, string? Level);
public record UpdateCourseRequest(string Name, string? Description, string? Category, string? Level);
public record SetCourseStatusRequest(CourseStatus Status);
