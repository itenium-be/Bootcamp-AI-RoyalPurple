using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// A learning resource linked to a goal (book, video, article, etc.).
/// </summary>
public class GoalResourceEntity
{
    [Key]
    public int Id { get; set; }

    public int GoalId { get; set; }

    public GoalEntity Goal { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    [Required]
    [MaxLength(500)]
    public required string Url { get; set; }

    [MaxLength(50)]
    public string Type { get; set; } = "article";
}
