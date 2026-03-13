using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// A skill in the competence centre roadmap.
/// Tier 1 = Foundation, Tier 2 = Core, Tier 3 = Advanced, Tier 4 = Expert
/// </summary>
public class SkillEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// 1 = Foundation, 2 = Core, 3 = Advanced, 4 = Expert
    /// </summary>
    public int Tier { get; set; }

    public int TeamId { get; set; }

    public TeamEntity Team { get; set; } = null!;
}
