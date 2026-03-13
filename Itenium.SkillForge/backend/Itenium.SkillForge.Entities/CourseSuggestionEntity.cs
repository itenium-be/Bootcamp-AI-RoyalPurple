using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Itenium.SkillForge.Entities;

public class CourseSuggestionEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public required string UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }

    public SuggestionStatus Status { get; set; } = SuggestionStatus.Pending;

    [MaxLength(450)]
    public string? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(1000)]
    public string? ReviewNote { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public override string ToString() => $"CourseSuggestion #{Id}: {Title} ({Status})";
}
