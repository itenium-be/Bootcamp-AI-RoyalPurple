using System.ComponentModel.DataAnnotations;

namespace Itenium.SkillForge.Entities;

public class UserActivityEntity
{
    [Key]
    [MaxLength(450)]
    public required string UserId { get; set; }

    public DateTime LastActivityAt { get; set; }
}
