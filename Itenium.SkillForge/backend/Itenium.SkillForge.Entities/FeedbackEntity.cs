using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

public class FeedbackEntity
{
    public int Id { get; set; }

    [Required, MaxLength(450)]
    public required string AuthorId { get; set; }

    [Required, MaxLength(450)]
    public required string RecipientId { get; set; }

    public int? CourseId { get; set; }

    [Required, MaxLength(2000)]
    public required string Content { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CourseEntity? Course { get; set; }

    public IList<FeedbackCommentEntity> Comments { get; set; } = [];
}
