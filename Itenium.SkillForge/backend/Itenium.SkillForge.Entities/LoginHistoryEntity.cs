using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

/// <summary>
/// Records a login event for a user.
/// </summary>
public class LoginHistoryEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public required string UserId { get; set; }

    public DateTime LoggedInAt { get; set; } = DateTime.UtcNow;
}
