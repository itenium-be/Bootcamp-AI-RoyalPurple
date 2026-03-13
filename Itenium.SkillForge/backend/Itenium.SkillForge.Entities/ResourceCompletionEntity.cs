using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Itenium.SkillForge.Entities;

/// <summary>Records that a learner has completed a course resource.</summary>
public class ResourceCompletionEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = "";

    public int ResourceId { get; set; }

    [ForeignKey(nameof(ResourceId))]
    public CourseResourceEntity Resource { get; set; } = null!;

    public DateTime CompletedAt { get; set; }
}
