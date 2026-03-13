using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/course/{courseId:int}/resource")]
[Authorize]
public class CourseResourceController : ControllerBase
{
    private readonly AppDbContext _db;

    public CourseResourceController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Get all resources for a course, ordered by display order.</summary>
    [HttpGet]
    public async Task<ActionResult<IList<CourseResourceEntity>>> GetResources(int courseId)
    {
        var courseExists = await _db.Courses.AnyAsync(c => c.Id == courseId);
        if (!courseExists)
            return NotFound();

        var resources = await _db.CourseResources
            .Where(r => r.CourseId == courseId)
            .OrderBy(r => r.Order)
            .ToListAsync();

        return Ok(resources);
    }

    /// <summary>Get a single resource for a course.</summary>
    [HttpGet("{resourceId:int}")]
    public async Task<ActionResult<CourseResourceEntity>> GetResource(int courseId, int resourceId)
    {
        var courseExists = await _db.Courses.AnyAsync(c => c.Id == courseId);
        if (!courseExists)
            return NotFound();

        var resource = await _db.CourseResources
            .FirstOrDefaultAsync(r => r.Id == resourceId && r.CourseId == courseId);

        if (resource == null)
            return NotFound();

        return Ok(resource);
    }

    /// <summary>Add a resource to a course.</summary>
    [HttpPost]
    [Authorize(Roles = "backoffice,manager")]
    public async Task<ActionResult<CourseResourceEntity>> CreateResource(int courseId, [FromBody] CreateCourseResourceRequest request)
    {
        var courseExists = await _db.Courses.AnyAsync(c => c.Id == courseId);
        if (!courseExists)
            return NotFound();

        var resource = new CourseResourceEntity
        {
            CourseId = courseId,
            Title = request.Title,
            Url = request.Url,
            Type = request.Type,
            Description = request.Description,
            DurationMinutes = request.DurationMinutes,
            Order = request.Order,
            SkillId = request.SkillId,
            ToLevel = request.ToLevel,
        };

        _db.CourseResources.Add(resource);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetResource), new { courseId, resourceId = resource.Id }, resource);
    }

    /// <summary>Update a resource on a course.</summary>
    [HttpPut("{resourceId:int}")]
    [Authorize(Roles = "backoffice,manager")]
    public async Task<ActionResult<CourseResourceEntity>> UpdateResource(int courseId, int resourceId, [FromBody] UpdateCourseResourceRequest request)
    {
        var resource = await _db.CourseResources
            .FirstOrDefaultAsync(r => r.Id == resourceId && r.CourseId == courseId);

        if (resource == null)
            return NotFound();

        resource.Title = request.Title;
        resource.Url = request.Url;
        resource.Type = request.Type;
        resource.Description = request.Description;
        resource.DurationMinutes = request.DurationMinutes;
        resource.Order = request.Order;
        resource.SkillId = request.SkillId;
        resource.ToLevel = request.ToLevel;

        await _db.SaveChangesAsync();

        return Ok(resource);
    }

    /// <summary>Remove a resource from a course.</summary>
    [HttpDelete("{resourceId:int}")]
    [Authorize(Roles = "backoffice,manager")]
    public async Task<IActionResult> DeleteResource(int courseId, int resourceId)
    {
        var resource = await _db.CourseResources
            .FirstOrDefaultAsync(r => r.Id == resourceId && r.CourseId == courseId);

        if (resource == null)
            return NotFound();

        _db.CourseResources.Remove(resource);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateCourseResourceRequest(
    string Title,
    string? Url,
    CourseResourceType Type,
    string? Description,
    int? DurationMinutes,
    int Order,
    int? SkillId,
    int? ToLevel);

public record UpdateCourseResourceRequest(
    string Title,
    string? Url,
    CourseResourceType Type,
    string? Description,
    int? DurationMinutes,
    int Order,
    int? SkillId,
    int? ToLevel);
