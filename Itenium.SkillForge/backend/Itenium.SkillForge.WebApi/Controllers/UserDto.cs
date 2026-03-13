namespace Itenium.SkillForge.WebApi.Controllers;

public record UserDto(
    string Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    IList<string> Roles,
    bool IsActive,
    IList<int> Teams,
    DateTime? LastLoginAt = null);
