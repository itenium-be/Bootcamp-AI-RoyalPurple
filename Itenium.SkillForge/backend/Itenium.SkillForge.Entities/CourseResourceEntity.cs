using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

public class CourseResourceEntity
{
    [Key]
    public int Id { get; set; }

    public int CourseId { get; set; }
    public CourseEntity Course { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    [MaxLength(500)]
    public string? Url { get; set; }

    public CourseResourceType Type { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Estimated time to complete in minutes.</summary>
    public int? DurationMinutes { get; set; }

    /// <summary>Display order within the course.</summary>
    public int Order { get; set; }

    /// <summary>Optional link to a skill this resource helps develop.</summary>
    public int? SkillId { get; set; }
    public SkillEntity? Skill { get; set; }

    /// <summary>Skill level this resource helps achieve.</summary>
    public int? ToLevel { get; set; }

    public override string ToString() => Title;
}
