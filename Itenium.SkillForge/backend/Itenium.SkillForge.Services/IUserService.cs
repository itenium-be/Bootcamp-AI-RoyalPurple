namespace Itenium.SkillForge.Services;

public record UserDto(string Id, string UserName, string Email, string FirstName, string LastName, string Role, int[] Teams);

public record CreateUserRequest(string UserName, string Email, string Password, string FirstName, string LastName, string Role, int[] Teams);

public record AssignRoleRequest(string Role);

public record AssignTeamsRequest(int[] TeamIds);

public interface IUserService
{
    Task<IList<UserDto>> GetAllUsersAsync();
    Task<IList<UserDto>> GetTeamMembersAsync(int[] teamIds);
    Task<IList<UserDto>> GetCoachesForTeamsAsync(int[] teamIds);
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<UserDto?> CreateUserAsync(CreateUserRequest request);
    Task<bool> AssignRoleAsync(string userId, string role);
    Task<bool> AssignTeamsAsync(string userId, int[] teamIds);
}
