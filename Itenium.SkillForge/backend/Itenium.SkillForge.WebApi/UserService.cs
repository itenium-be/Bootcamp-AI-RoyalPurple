using System.Globalization;
using System.Security.Claims;
using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Identity;

namespace Itenium.SkillForge.WebApi;

public class UserService : IUserService
{
    private readonly UserManager<ForgeUser> _userManager;

    public UserService(UserManager<ForgeUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IList<UserDto>> GetAllUsersAsync()
    {
        var users = _userManager.Users.ToList();
        var result = new List<UserDto>();
        foreach (var user in users)
        {
            result.Add(await ToDto(user));
        }

        return result;
    }

    public async Task<IList<UserDto>> GetTeamMembersAsync(ICollection<int> teamIds)
    {
        var found = new HashSet<ForgeUser>();
        foreach (var teamId in teamIds)
        {
            var claim = new Claim("team", teamId.ToString(CultureInfo.InvariantCulture));
            var users = await _userManager.GetUsersForClaimAsync(claim);
            foreach (var user in users)
            {
                found.Add(user);
            }
        }

        var result = new List<UserDto>();
        foreach (var user in found)
        {
            result.Add(await ToDto(user));
        }

        return result;
    }

    public async Task<IList<UserDto>> GetCoachesForTeamsAsync(ICollection<int> teamIds)
    {
        var found = new HashSet<ForgeUser>();
        foreach (var teamId in teamIds)
        {
            var claim = new Claim("team", teamId.ToString(CultureInfo.InvariantCulture));
            var users = await _userManager.GetUsersForClaimAsync(claim);
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("manager", StringComparer.Ordinal))
                {
                    found.Add(user);
                }
            }
        }

        var result = new List<UserDto>();
        foreach (var user in found)
        {
            result.Add(await ToDto(user));
        }

        return result;
    }

    public async Task<IList<UserDto>> GetAdminUsersAsync()
    {
        var users = await _userManager.GetUsersInRoleAsync("backoffice");
        var result = new List<UserDto>();
        foreach (var user in users)
        {
            result.Add(await ToDto(user));
        }

        return result;
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user == null ? null : await ToDto(user);
    }

    public async Task<CreateUserResult> CreateUserAsync(CreateUserRequest request)
    {
        var user = new ForgeUser
        {
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = true,
            FirstName = request.FirstName,
            LastName = request.LastName,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .Select(e => new UserError(e.Code, e.Description))
                .ToList();
            return CreateUserResult.Failure(errors);
        }

        await _userManager.AddToRoleAsync(user, request.Role);
        foreach (var teamId in request.Teams)
        {
            await _userManager.AddClaimAsync(user, new Claim("team", teamId.ToString(CultureInfo.InvariantCulture)));
        }

        return CreateUserResult.Success(await ToDto(user));
    }

    public async Task<bool> AssignRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, role);
        return true;
    }

    public async Task<bool> AssignTeamsAsync(string userId, ICollection<int> teamIds)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var currentTeamClaims = (await _userManager.GetClaimsAsync(user))
            .Where(c => c.Type == "team")
            .ToList();
        await _userManager.RemoveClaimsAsync(user, currentTeamClaims);

        foreach (var teamId in teamIds)
        {
            await _userManager.AddClaimAsync(user, new Claim("team", teamId.ToString(CultureInfo.InvariantCulture)));
        }

        return true;
    }

    private async Task<UserDto> ToDto(ForgeUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);
        var teams = claims
            .Where(c => c.Type == "team")
            .Select(c => int.Parse(c.Value, CultureInfo.InvariantCulture))
            .ToArray();

        return new UserDto(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.FirstName ?? string.Empty,
            user.LastName ?? string.Empty,
            roles.FirstOrDefault() ?? string.Empty,
            teams);
    }
}
