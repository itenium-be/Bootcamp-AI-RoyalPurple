using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Records a learner's enrollment in a course.
/// </summary>
public class CourseEnrollmentEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string UserId { get; set; }

    public int CourseId { get; set; }

    [ForeignKey(nameof(CourseId))]
    public CourseEntity? Course { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
}
