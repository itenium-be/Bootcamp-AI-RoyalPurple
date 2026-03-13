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
public class FeedbackController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    public FeedbackController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>
    /// Get feedback. Backoffice sees all, others see their own. Optional courseId filter.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<FeedbackEntity>>> GetFeedback([FromQuery] int? courseId)
    {
        var query = _db.Feedback.Include(f => f.Course).AsQueryable();

        if (!_user.IsBackOffice)
        {
            query = query.Where(f => f.UserId == _user.UserId);
        }

        if (courseId.HasValue)
        {
            query = query.Where(f => f.CourseId == courseId.Value);
        }

        return await query.ToListAsync();
    }

    /// <summary>
    /// Submit feedback for a course.
    /// </summary>
    [HttpPost("{courseId:int}")]
    public async Task<ActionResult<FeedbackEntity>> SubmitFeedback(int courseId, [FromBody] FeedbackRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
        {
            return BadRequest("Rating must be between 1 and 5.");
        }

        var course = await _db.Courses.FindAsync(courseId);
        if (course == null)
        {
            return NotFound();
        }

        var existing = await _db.Feedback
            .FirstOrDefaultAsync(f => f.UserId == _user.UserId && f.CourseId == courseId);
        if (existing != null)
        {
            return BadRequest("Feedback already submitted for this course.");
        }

        var feedback = new FeedbackEntity
        {
            UserId = _user.UserId!,
            CourseId = courseId,
            Rating = request.Rating,
            Comment = request.Comment,
        };
        _db.Feedback.Add(feedback);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetFeedback), new { id = feedback.Id }, feedback);
    }

    /// <summary>
    /// Delete feedback (own only, unless backoffice).
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteFeedback(int id)
    {
        var feedback = await _db.Feedback.FindAsync(id);
        if (feedback == null)
        {
            return NotFound();
        }

        if (!_user.IsBackOffice && feedback.UserId != _user.UserId)
        {
            return Forbid();
        }

        _db.Feedback.Remove(feedback);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
