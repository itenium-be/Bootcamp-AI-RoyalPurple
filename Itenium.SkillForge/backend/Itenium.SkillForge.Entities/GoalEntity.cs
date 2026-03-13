using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// A coach-assigned growth goal for a consultant. Tracks the skill to work on,
/// the current and target niveau, and the deadline.
/// </summary>
public class GoalEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public required string ConsultantId { get; set; }

    public int SkillId { get; set; }

    public SkillEntity Skill { get; set; } = null!;

    /// <summary>Current niveau of the consultant for this skill (0 if not yet started).</summary>
    public int CurrentLevel { get; set; }

    /// <summary>Target niveau the consultant should reach.</summary>
    public int TargetLevel { get; set; }

    public DateTime Deadline { get; set; }

    [Required]
    [MaxLength(450)]
    public required string CoachId { get; set; }

    public IList<GoalResourceEntity> Resources { get; set; } = [];

    public IList<ReadinessFlagEntity> ReadinessFlags { get; set; } = [];
}
