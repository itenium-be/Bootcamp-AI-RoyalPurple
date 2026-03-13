namespace Itenium.SkillForge.Services;

public record UserDto(string Id, string UserName, string Email, string FirstName, string LastName, string Role, int[] Teams);

public record CreateUserRequest(string UserName, string Email, string Password, string FirstName, string LastName, string Role, int[] Teams);

public record UserError(string Code, string Description);

public record CreateUserResult(UserDto? User, IReadOnlyList<UserError> Errors)
{
    public bool Succeeded => User != null;

    public static CreateUserResult Success(UserDto user) => new(user, []);
    public static CreateUserResult Failure(IReadOnlyList<UserError> errors) => new(null, errors);
}

public record AssignRoleRequest(string Role);

public record AssignTeamsRequest(int[] TeamIds);

public interface IUserService
{
    Task<IList<UserDto>> GetAllUsersAsync();
    Task<IList<UserDto>> GetTeamMembersAsync(ICollection<int> teamIds);
    Task<IList<UserDto>> GetCoachesForTeamsAsync(ICollection<int> teamIds);
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<CreateUserResult> CreateUserAsync(CreateUserRequest request);
    Task<bool> AssignRoleAsync(string userId, string role);
    Task<bool> AssignTeamsAsync(string userId, ICollection<int> teamIds);
}
