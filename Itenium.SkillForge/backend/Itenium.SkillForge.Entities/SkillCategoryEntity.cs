using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Groups skills into logical categories (e.g., "Backend .NET", "General Soft Skills").
/// Universal categories have no TeamId; CC-specific categories are linked to a team.
/// </summary>
public class SkillCategoryEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    /// <summary>
    /// Null = universal (itenium-wide). Set = competence-centre-specific.
    /// </summary>
    public int? TeamId { get; set; }

    public TeamEntity? Team { get; set; }

    public IList<SkillEntity> Skills { get; set; } = [];

    public override string ToString() => Name;
}
