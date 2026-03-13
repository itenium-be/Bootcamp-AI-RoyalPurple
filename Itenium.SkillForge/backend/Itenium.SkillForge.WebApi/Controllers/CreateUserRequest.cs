namespace Itenium.SkillForge.WebApi.Controllers;

public record CreateUserRequest(string Username, string Email, string FirstName, string LastName, string Password);
