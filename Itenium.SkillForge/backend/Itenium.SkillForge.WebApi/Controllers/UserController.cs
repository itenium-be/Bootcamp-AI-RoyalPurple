using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ISkillForgeUser _currentUser;

    public UserController(IUserService userService, ISkillForgeUser currentUser)
    {
        _userService = userService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get users. Backoffice gets all users; managers get their team members; learners get only themselves.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IList<UserDto>>> GetUsers()
    {
        if (_currentUser.IsBackOffice)
        {
            return Ok(await _userService.GetAllUsersAsync());
        }

        if (_currentUser.Teams.Count > 0)
        {
            return Ok(await _userService.GetTeamMembersAsync(_currentUser.Teams.ToArray()));
        }

        var self = await _userService.GetUserByIdAsync(_currentUser.UserId!);
        IList<UserDto> result = self == null ? [] : [self];
        return Ok(result);
    }

    /// <summary>
    /// Get the coaches of the current user's teams.
    /// </summary>
    [HttpGet("coach")]
    public async Task<ActionResult<IList<UserDto>>> GetCoaches()
    {
        if (_currentUser.Teams.Count == 0)
        {
            return Ok(new List<UserDto>());
        }

        return Ok(await _userService.GetCoachesForTeamsAsync(_currentUser.Teams.ToArray()));
    }

    /// <summary>
    /// Get the currently authenticated user's profile.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var user = await _userService.GetUserByIdAsync(_currentUser.UserId!);
        return user == null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Get a user by ID (Admin only).
    /// </summary>
    [HttpGet("{userId}")]
    [Authorize(Roles = "backoffice")]
    public async Task<ActionResult<UserDto>> GetUserById(string userId)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        return user == null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Create a new user account with role and team assignment (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "backoffice")]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequest request)
    {
        var user = await _userService.CreateUserAsync(request);
        if (user == null)
        {
            return BadRequest();
        }

        return CreatedAtAction(nameof(GetUserById), new { userId = user.Id }, user);
    }

    /// <summary>
    /// Assign a role to a user (Admin only).
    /// </summary>
    [HttpPut("{userId}/role")]
    [Authorize(Roles = "backoffice")]
    public async Task<IActionResult> AssignRole(string userId, AssignRoleRequest request)
    {
        var success = await _userService.AssignRoleAsync(userId, request.Role);
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Assign team memberships to a user (Admin only).
    /// </summary>
    [HttpPut("{userId}/teams")]
    [Authorize(Roles = "backoffice")]
    public async Task<IActionResult> AssignTeams(string userId, AssignTeamsRequest request)
    {
        var success = await _userService.AssignTeamsAsync(userId, request.TeamIds);
        return success ? NoContent() : NotFound();
    }
}
