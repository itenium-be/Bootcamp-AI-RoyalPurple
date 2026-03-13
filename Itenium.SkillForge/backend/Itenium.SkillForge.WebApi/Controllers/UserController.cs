using System.Globalization;
using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ForgeUser> _userManager;
    private readonly ISkillForgeUser _user;

    public UserController(AppDbContext db, UserManager<ForgeUser> userManager, ISkillForgeUser user)
    {
        _db = db;
        _userManager = userManager;
        _user = user;
    }

    /// <summary>
    /// Get all users with their roles, teams, and last login. Backoffice only.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var users = await _db.Users.ToListAsync();
        var userIds = users.Select(u => u.Id).ToList();

        var userRoles = await _db.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync();

        var teamClaims = await _db.UserClaims
            .Where(c => userIds.Contains(c.UserId) && c.ClaimType == "team")
            .ToListAsync();

        var lastLogins = await _db.LoginHistory
            .Where(l => userIds.Contains(l.UserId))
            .GroupBy(l => l.UserId)
            .Select(g => new { UserId = g.Key, LastLoginAt = g.Max(l => l.LoggedInAt) })
            .ToListAsync();

        return users.Select(u => new UserDto(
            u.Id,
            u.UserName!,
            u.Email!,
            u.FirstName ?? string.Empty,
            u.LastName ?? string.Empty,
            userRoles.Where(ur => ur.UserId == u.Id).Select(ur => ur.Name!).ToList(),
            u.LockoutEnd == null || u.LockoutEnd < DateTimeOffset.UtcNow,
            teamClaims.Where(c => c.UserId == u.Id)
                      .Select(c => int.Parse(c.ClaimValue!, CultureInfo.InvariantCulture))
                      .ToList(),
            lastLogins.FirstOrDefault(l => l.UserId == u.Id)?.LastLoginAt
        )).ToList();
    }

    /// <summary>
    /// Update the roles of a user. Backoffice only.
    /// </summary>
    [HttpPut("{id}/roles")]
    public async Task<ActionResult> UpdateRoles(string id, [FromBody] UpdateRolesRequest request)
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        if (request.Roles.Count > 0)
        {
            await _userManager.AddToRolesAsync(user, request.Roles);
        }

        return NoContent();
    }

    /// <summary>
    /// Create a new user. Backoffice only.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var user = new ForgeUser
        {
            UserName = request.Username,
            NormalizedUserName = request.Username.ToUpperInvariant(),
            Email = request.Email,
            NormalizedEmail = request.Email.ToUpperInvariant(),
            FirstName = request.FirstName,
            LastName = request.LastName,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        var dto = new UserDto(user.Id, user.UserName!, user.Email!, user.FirstName ?? string.Empty, user.LastName ?? string.Empty, [], true, []);
        return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, dto);
    }

    /// <summary>
    /// Activate or deactivate a user. Backoffice only.
    /// </summary>
    [HttpPut("{id}/active")]
    public async Task<ActionResult> SetActive(string id, [FromBody] SetActiveRequest request)
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (!request.IsActive)
        {
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
        }

        return NoContent();
    }

    /// <summary>
    /// Record a login event for the current user.
    /// </summary>
    [HttpPost("activity")]
    public async Task<ActionResult> RecordActivity()
    {
        _db.LoginHistory.Add(new LoginHistoryEntity
        {
            UserId = _user.UserId!,
            LoggedInAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Get login history for a user. Backoffice only.
    /// </summary>
    [HttpGet("{id}/history")]
    public async Task<ActionResult<List<LoginHistoryEntity>>> GetLoginHistory(string id)
    {
        if (!_user.IsBackOffice)
        {
            return Forbid();
        }

        var history = await _db.LoginHistory
            .Where(l => l.UserId == id)
            .OrderByDescending(l => l.LoggedInAt)
            .ToListAsync();

        return history;
    }
}
