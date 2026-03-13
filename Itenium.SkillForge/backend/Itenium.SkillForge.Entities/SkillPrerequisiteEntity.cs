namespace Itenium.SkillForge.Entities;

/// <summary>
/// Declares that a skill requires another skill as a prerequisite.
/// </summary>
public class SkillPrerequisiteEntity
{
    public int SkillId { get; set; }

    public SkillEntity Skill { get; set; } = null!;

    public int PrerequisiteSkillId { get; set; }

    public SkillEntity PrerequisiteSkill { get; set; } = null!;
}
