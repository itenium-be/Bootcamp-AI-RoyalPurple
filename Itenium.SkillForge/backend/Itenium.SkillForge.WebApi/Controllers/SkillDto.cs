namespace Itenium.SkillForge.WebApi.Controllers;

public record SkillDto(int Id, string Name, string? Description, int Tier, int TeamId);
