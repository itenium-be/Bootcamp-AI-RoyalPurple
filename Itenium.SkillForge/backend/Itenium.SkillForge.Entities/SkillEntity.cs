using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// A skill in the catalogue. LevelCount 1 = checkbox (known/not known), 2-7 = progression levels.
/// Tier is used by the roadmap feature for progressive disclosure (1=Foundation → 4=Expert).
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
    /// Number of progression levels. 1 = checkbox, 2-7 = progression.
    /// </summary>
    public int LevelCount { get; set; } = 1;

    /// <summary>
    /// Roadmap tier for progressive disclosure. 1 = Foundation, 2 = Core, 3 = Advanced, 4 = Expert.
    /// </summary>
    public int Tier { get; set; } = 1;

    public int CategoryId { get; set; }

    public SkillCategoryEntity Category { get; set; } = null!;

    public IList<SkillLevelEntity> Levels { get; set; } = [];

    public IList<SkillPrerequisiteEntity> Prerequisites { get; set; } = [];

    public override string ToString() => Name;
}
