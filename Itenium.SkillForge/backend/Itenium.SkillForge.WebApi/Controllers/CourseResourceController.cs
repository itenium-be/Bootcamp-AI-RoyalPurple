using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
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
    private readonly ISkillForgeUser _currentUser;

    public CourseResourceController(AppDbContext db, ISkillForgeUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Get all resources for a course, ordered by display order.</summary>
    [HttpGet]
    public async Task<ActionResult<IList<CourseResourceDto>>> GetResources(int courseId)
    {
        var courseExists = await _db.Courses.AnyAsync(c => c.Id == courseId);
        if (!courseExists)
            return NotFound();

        var userId = _currentUser.UserId!;
        var completedIds = await _db.ResourceCompletions
            .Where(c => c.UserId == userId)
            .Select(c => c.ResourceId)
            .ToHashSetAsync();

        var resources = await _db.CourseResources
            .Where(r => r.CourseId == courseId)
            .Include(r => r.Skill)
            .OrderBy(r => r.Order)
            .Select(r => new CourseResourceDto(
                r.Id, r.CourseId, r.Title, r.Url, r.Type, r.Description,
                r.DurationMinutes, r.Order, r.SkillId, r.Skill != null ? r.Skill.Name : null,
                r.ToLevel, completedIds.Contains(r.Id)))
            .ToListAsync();

        return Ok(resources);
    }

    /// <summary>Get a single resource for a course.</summary>
    [HttpGet("{resourceId:int}")]
    public async Task<ActionResult<CourseResourceEntity>> GetResource(int courseId, int resourceId)
    {
        var resource = await _db.CourseResources
            .FirstOrDefaultAsync(r => r.Id == resourceId && r.CourseId == courseId);

        if (resource == null)
            return NotFound();

        return Ok(resource);
    }

    /// <summary>Add a resource to a course.</summary>
    [HttpPost]
    [Authorize(Roles = "backoffice,manager")]
    public async Task<ActionResult<CourseResourceEntity>> CreateResource(int courseId, [FromBody] CourseResourceRequest request)
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
    public async Task<ActionResult<CourseResourceEntity>> UpdateResource(int courseId, int resourceId, [FromBody] CourseResourceRequest request)
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

    /// <summary>Mark a resource as completed by the current user.</summary>
    [HttpPost("{resourceId:int}/complete")]
    public async Task<IActionResult> MarkComplete(int courseId, int resourceId)
    {
        var resourceExists = await _db.CourseResources
            .AnyAsync(r => r.Id == resourceId && r.CourseId == courseId);
        if (!resourceExists)
            return NotFound();

        var userId = _currentUser.UserId!;
        var alreadyCompleted = await _db.ResourceCompletions
            .AnyAsync(c => c.UserId == userId && c.ResourceId == resourceId);
        if (alreadyCompleted)
            return Conflict();

        _db.ResourceCompletions.Add(new ResourceCompletionEntity
        {
            UserId = userId,
            ResourceId = resourceId,
            CompletedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Remove completion record for a resource.</summary>
    [HttpDelete("{resourceId:int}/complete")]
    public async Task<IActionResult> UnmarkComplete(int courseId, int resourceId)
    {
        var userId = _currentUser.UserId!;
        var completion = await _db.ResourceCompletions
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ResourceId == resourceId);

        if (completion == null)
            return NotFound();

        _db.ResourceCompletions.Remove(completion);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public record CourseResourceDto(
    int Id,
    int CourseId,
    string Title,
    string? Url,
    CourseResourceType Type,
    string? Description,
    int? DurationMinutes,
    int Order,
    int? SkillId,
    string? SkillName,
    int? ToLevel,
    bool IsCompleted);

public record CourseResourceRequest(
    string Title,
    string? Url,
    CourseResourceType Type,
    string? Description,
    int? DurationMinutes,
    int Order,
    int? SkillId,
    int? ToLevel);
