using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Signals that a consultant is ready for a skill validation on a goal.
/// One flag per goal (GoalId is the primary key).
/// </summary>
public class ReadinessFlagEntity
{
    [Key]
    public int GoalId { get; set; }

    public GoalEntity Goal { get; set; } = null!;

    public DateTime RaisedAt { get; set; }
}
