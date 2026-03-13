using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Assigns a course as mandatory or optional to a team or an individual member.
/// Either TeamId or UserId must be set, not both.
/// </summary>
public class CourseAssignmentEntity
{
    [Key]
    public int Id { get; set; }

    public int CourseId { get; set; }

    [ForeignKey(nameof(CourseId))]
    public CourseEntity Course { get; set; } = null!;

    public int? TeamId { get; set; }

    [ForeignKey(nameof(TeamId))]
    public TeamEntity? Team { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }

    public bool IsRequired { get; set; }

    [Required]
    [MaxLength(450)]
    public required string AssignedById { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
