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
    /// List feedback visible to the current user.
    /// Backoffice sees all; others see feedback they authored or received.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<FeedbackDto>>> GetFeedback()
    {
        var userId = _user.UserId!;
        var query = _db.Feedbacks.AsQueryable();

        if (!_user.IsBackOffice)
            query = query.Where(f => f.AuthorId == userId || f.RecipientId == userId);

        return await query
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FeedbackDto(f.Id, f.AuthorId, f.RecipientId, f.CourseId, f.Content, f.CreatedAt))
            .ToListAsync();
    }

    /// <summary>
    /// Get feedback details including comments.
    /// Only visible to the author, recipient, or backoffice.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<FeedbackDetailDto>> GetFeedbackById(int id)
    {
        var userId = _user.UserId!;
        var feedback = await _db.Feedbacks
            .Include(f => f.Comments)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (feedback == null) return NotFound();
        if (!_user.IsBackOffice && feedback.AuthorId != userId && feedback.RecipientId != userId)
            return Forbid();

        IList<FeedbackCommentDto> comments = feedback.Comments
            .OrderBy(c => c.CreatedAt)
            .Select(c => new FeedbackCommentDto(c.Id, c.AuthorId, c.Content, c.CreatedAt))
            .ToList();

        return Ok(new FeedbackDetailDto(feedback.Id, feedback.AuthorId, feedback.RecipientId, feedback.CourseId, feedback.Content, feedback.CreatedAt, comments));
    }

    /// <summary>
    /// Create feedback. Coach creates for admin; learner creates for their coach.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<FeedbackDto>> CreateFeedback([FromBody] CreateFeedbackRequest request)
    {
        var feedback = new FeedbackEntity
        {
            AuthorId = _user.UserId!,
            RecipientId = request.RecipientId,
            CourseId = request.CourseId,
            Content = request.Content,
        };

        _db.Feedbacks.Add(feedback);
        await _db.SaveChangesAsync();

        var dto = new FeedbackDto(feedback.Id, feedback.AuthorId, feedback.RecipientId, feedback.CourseId, feedback.Content, feedback.CreatedAt);
        return CreatedAtAction(nameof(GetFeedbackById), new { id = feedback.Id }, dto);
    }

    /// <summary>
    /// Add a comment to a feedback item. Only the author or recipient can comment.
    /// </summary>
    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<FeedbackCommentDto>> AddComment(int id, [FromBody] CreateCommentRequest request)
    {
        var userId = _user.UserId!;
        var feedback = await _db.Feedbacks.FindAsync(id);

        if (feedback == null) return NotFound();
        if (!_user.IsBackOffice && feedback.AuthorId != userId && feedback.RecipientId != userId)
            return Forbid();

        var comment = new FeedbackCommentEntity
        {
            FeedbackId = feedback.Id,
            AuthorId = userId,
            Content = request.Content,
        };

        _db.FeedbackComments.Add(comment);
        await _db.SaveChangesAsync();

        var dto = new FeedbackCommentDto(comment.Id, comment.AuthorId, comment.Content, comment.CreatedAt);
        return CreatedAtAction(nameof(GetFeedbackById), new { id = feedback.Id }, dto);
    }
}

public record FeedbackDto(int Id, string AuthorId, string RecipientId, int? CourseId, string Content, DateTime CreatedAt);
public record FeedbackDetailDto(int Id, string AuthorId, string RecipientId, int? CourseId, string Content, DateTime CreatedAt, IList<FeedbackCommentDto> Comments);
public record FeedbackCommentDto(int Id, string AuthorId, string Content, DateTime CreatedAt);
public record CreateFeedbackRequest(string RecipientId, string Content, int? CourseId);
public record CreateCommentRequest(string Content);
