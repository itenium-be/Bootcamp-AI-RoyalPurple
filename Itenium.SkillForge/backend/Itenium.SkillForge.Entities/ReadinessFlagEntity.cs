using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// A readiness flag raised by a consultant on a goal, signalling to their coach
/// that they are ready for review/evaluation on that skill.
/// Maximum one active (unresolved) flag per goal at a time.
/// </summary>
public class ReadinessFlagEntity
{
    [Key]
    public int Id { get; set; }

    public int GoalId { get; set; }

    public GoalEntity Goal { get; set; } = null!;

    public DateTime RaisedAt { get; set; }

    /// <summary>Set when the coach (or consultant) resolves/dismisses the flag.</summary>
    public DateTime? ResolvedAt { get; set; }

    public bool IsActive => ResolvedAt == null;
}
