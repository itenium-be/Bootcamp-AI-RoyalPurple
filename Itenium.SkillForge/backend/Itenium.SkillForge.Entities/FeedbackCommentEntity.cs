using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

public class FeedbackCommentEntity
{
    public int Id { get; set; }

    public int FeedbackId { get; set; }

    [Required, MaxLength(450)]
    public required string AuthorId { get; set; }

    [Required, MaxLength(2000)]
    public required string Content { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public FeedbackEntity Feedback { get; set; } = null!;
}
