using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Represents the assignment of a consultant (learner) to a competence centre profile (team).
/// </summary>
public class ConsultantProfileEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public required string UserId { get; set; }

    public int TeamId { get; set; }

    [ForeignKey(nameof(TeamId))]
    public TeamEntity? Team { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
