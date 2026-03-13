using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Course master data managed by central management.
/// </summary>
public class CourseEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(50)]
    public string? Level { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional competence centre profile this course belongs to. Null means available for all profiles.
    /// </summary>
    public int? TeamId { get; set; }

    [ForeignKey(nameof(TeamId))]
    public TeamEntity? Team { get; set; }

    public override string ToString() => $"{Name} ({Category})";
}
