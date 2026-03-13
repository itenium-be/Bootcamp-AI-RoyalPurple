namespace Itenium.SkillForge.Entities;

public class TeamAssignmentEntity
{
    public int Id { get; set; }

    public int TeamId { get; set; }

    public TeamEntity Team { get; set; } = null!;

    public int CourseId { get; set; }

    public CourseEntity Course { get; set; } = null!;

    /// <summary>
    /// Null = assigned to the entire team. Non-null = assigned to a specific member.
    /// </summary>
    public string? UserId { get; set; }

    public bool IsMandatory { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
