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
public class SuggestionController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISkillForgeUser _user;

    public SuggestionController(AppDbContext db, ISkillForgeUser user)
    {
        _db = db;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult<List<CourseSuggestionEntity>>> GetSuggestions()
    {
        var query = _db.CourseSuggestions.AsQueryable();
        if (!_user.IsBackOffice && !_user.IsManager)
        {
            query = query.Where(s => s.UserId == _user.UserId);
        }

        return await query.OrderByDescending(s => s.SubmittedAt).ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<CourseSuggestionEntity>> Submit([FromBody] SuggestionRequest request)
    {
        var suggestion = new CourseSuggestionEntity
        {
            UserId = _user.UserId!,
            Title = request.Title,
            Description = request.Description,
            Reason = request.Reason,
        };

        _db.CourseSuggestions.Add(suggestion);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSuggestions), new { id = suggestion.Id }, suggestion);
    }

    [HttpPut("{id:int}/review")]
    public async Task<ActionResult> Review(int id, [FromBody] ReviewSuggestionRequest request)
    {
        if (!_user.IsBackOffice && !_user.IsManager)
        {
            return Forbid();
        }

        var suggestion = await _db.CourseSuggestions.FindAsync(id);
        if (suggestion == null)
        {
            return NotFound();
        }

        suggestion.Status = request.Status;
        suggestion.ReviewedBy = _user.UserId;
        suggestion.ReviewedAt = DateTime.UtcNow;
        suggestion.ReviewNote = request.ReviewNote;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var suggestion = await _db.CourseSuggestions.FindAsync(id);
        if (suggestion == null)
        {
            return NotFound();
        }

        if (!_user.IsBackOffice && suggestion.UserId != _user.UserId)
        {
            return Forbid();
        }

        _db.CourseSuggestions.Remove(suggestion);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
