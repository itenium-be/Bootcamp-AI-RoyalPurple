using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// A coach-assigned learning goal for a consultant (FR16, FR17).
/// </summary>
public class GoalEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public required string SkillName { get; set; }

    [Required]
    [MaxLength(200)]
    public required string ConsultantUserId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string CreatedByCoachId { get; set; }

    public int CurrentNiveau { get; set; }

    public int TargetNiveau { get; set; }

    public DateTime Deadline { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the consultant raised a readiness flag (FR18, FR19). Null if not raised.
    /// </summary>
    public DateTime? ReadinessFlagRaisedAt { get; set; }

    /// <summary>
    /// Optional comma-separated or newline-separated URLs / notes for linked resources (FR16).
    /// </summary>
    public string? LinkedResources { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public override string ToString() => $"{SkillName} ({CurrentNiveau}→{TargetNiveau})";
}
