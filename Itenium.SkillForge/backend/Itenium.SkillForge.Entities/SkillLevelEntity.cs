using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Describes what a consultant knows at a specific level of a skill.
/// Level 1 = entry, Level N = expert.
/// </summary>
public class SkillLevelEntity
{
    [Key]
    public int Id { get; set; }

    public int SkillId { get; set; }

    public SkillEntity Skill { get; set; } = null!;

    /// <summary>
    /// 1-based level number (1 = entry, up to LevelCount).
    /// </summary>
    public int Level { get; set; }

    [Required]
    [MaxLength(500)]
    public required string Descriptor { get; set; }

    public override string ToString() => $"Level {Level}: {Descriptor}";
}
