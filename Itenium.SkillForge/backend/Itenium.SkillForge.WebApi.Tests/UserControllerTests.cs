using System.Globalization;
using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class UserControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _currentUser = null!;
#pragma warning disable NUnit1032
    private UserManager<ForgeUser> _userManager = null!;
#pragma warning restore NUnit1032
    private UserController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _currentUser = Substitute.For<ISkillForgeUser>();
        _currentUser.IsBackOffice.Returns(true);

        var store = Substitute.For<IUserStore<ForgeUser>>();
        _userManager = Substitute.For<UserManager<ForgeUser>>(
            store,
            Substitute.For<IOptions<IdentityOptions>>(),
            null, null, null, null, null, null,
            Substitute.For<ILogger<UserManager<ForgeUser>>>());

        _sut = new UserController(Db, _userManager, _currentUser);
    }

    private async Task<ForgeUser> SeedUser(string username, string? role = null, int[]? teams = null, bool locked = false)
    {
        var user = new ForgeUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            Email = $"{username}@test.local",
            NormalizedEmail = $"{username}@test.local".ToUpperInvariant(),
            FirstName = username,
            LastName = "Test",
            LockoutEnabled = locked,
            LockoutEnd = locked ? DateTimeOffset.MaxValue : null,
        };
        Db.Users.Add(user);

        if (role != null)
        {
            var normalizedRole = role.ToUpperInvariant();
#pragma warning disable CA1862
            var existingRole = Db.Roles.FirstOrDefault(r => r.NormalizedName == normalizedRole);
#pragma warning restore CA1862
            if (existingRole == null)
            {
                existingRole = new IdentityRole { Id = Guid.NewGuid().ToString(), Name = role, NormalizedName = role.ToUpperInvariant() };
                Db.Roles.Add(existingRole);
            }
            Db.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = existingRole.Id });
        }

        foreach (var teamId in teams ?? [])
        {
            Db.UserClaims.Add(new IdentityUserClaim<string>
            {
                UserId = user.Id,
                ClaimType = "team",
                ClaimValue = teamId.ToString(CultureInfo.InvariantCulture)
            });
        }

        await Db.SaveChangesAsync();
        return user;
    }

    // ─── GetUsers ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetUsers_NonBackOffice_ReturnsForbid()
    {
        _currentUser.IsBackOffice.Returns(false);
        var result = await _sut.GetUsers();
        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task GetUsers_BackOffice_ReturnsAllUsers()
    {
        await SeedUser("alice", "manager");
        await SeedUser("bob", "learner");

        var result = await _sut.GetUsers();

        var users = result.Value!;
        Assert.That(users.Select(u => u.Username), Contains.Item("alice"));
        Assert.That(users.Select(u => u.Username), Contains.Item("bob"));
    }

    [Test]
    public async Task GetUsers_IncludesRoles()
    {
        await SeedUser("alice", "manager");

        var result = await _sut.GetUsers();

        var alice = result.Value!.Single(u => u.Username == "alice");
        Assert.That(alice.Roles, Contains.Item("manager"));
    }

    [Test]
    public async Task GetUsers_IncludesTeamClaims()
    {
        await SeedUser("alice", teams: [1, 2]);

        var result = await _sut.GetUsers();

        var alice = result.Value!.Single(u => u.Username == "alice");
        Assert.That(alice.Teams, Contains.Item(1));
        Assert.That(alice.Teams, Contains.Item(2));
    }

    [Test]
    public async Task GetUsers_ActiveUser_IsActiveTrue()
    {
        await SeedUser("alice");

        var result = await _sut.GetUsers();

        var alice = result.Value!.Single(u => u.Username == "alice");
        Assert.That(alice.IsActive, Is.True);
    }

    [Test]
    public async Task GetUsers_LockedUser_IsActiveFalse()
    {
        await SeedUser("alice", locked: true);

        var result = await _sut.GetUsers();

        var alice = result.Value!.Single(u => u.Username == "alice");
        Assert.That(alice.IsActive, Is.False);
    }

    // ─── UpdateRoles ─────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateRoles_NonBackOffice_ReturnsForbid()
    {
        _currentUser.IsBackOffice.Returns(false);
        var result = await _sut.UpdateRoles("id", new UpdateRolesRequest(["manager"]));
        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task UpdateRoles_UserNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync("nonexistent").Returns((ForgeUser?)null);
        var result = await _sut.UpdateRoles("nonexistent", new UpdateRolesRequest(["manager"]));
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task UpdateRoles_BackOffice_RemovesOldAndAddsNewRoles()
    {
        var user = new ForgeUser { Id = "u1", UserName = "alice", FirstName = "Alice", LastName = "Test" };
        _userManager.FindByIdAsync("u1").Returns(user);
        _userManager.GetRolesAsync(user).Returns(["learner"]);
        _userManager.RemoveFromRolesAsync(user, Arg.Any<IEnumerable<string>>()).Returns(IdentityResult.Success);
        _userManager.AddToRolesAsync(user, Arg.Any<IEnumerable<string>>()).Returns(IdentityResult.Success);

        var result = await _sut.UpdateRoles("u1", new UpdateRolesRequest(["manager"]));

        Assert.That(result, Is.TypeOf<NoContentResult>());
        await _userManager.Received(1).RemoveFromRolesAsync(user, Arg.Is<IEnumerable<string>>(r => r.Contains("learner")));
        await _userManager.Received(1).AddToRolesAsync(user, Arg.Is<IEnumerable<string>>(r => r.Contains("manager")));
    }

    // ─── CreateUser ──────────────────────────────────────────────────────────

    [Test]
    public async Task CreateUser_NonBackOffice_ReturnsForbid()
    {
        _currentUser.IsBackOffice.Returns(false);
        var result = await _sut.CreateUser(new CreateUserRequest("alice", "alice@test.local", "Alice", "Test", "Pass@1234"));
        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task CreateUser_BackOffice_CreatesAndReturnsUser()
    {
        var request = new CreateUserRequest("newuser", "new@test.local", "New", "User", "Pass@1234");
        _userManager.CreateAsync(Arg.Any<ForgeUser>(), request.Password)
            .Returns(IdentityResult.Success);

        var result = await _sut.CreateUser(request);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var dto = created!.Value as UserDto;
        Assert.That(dto!.Username, Is.EqualTo("newuser"));
        Assert.That(dto.Email, Is.EqualTo("new@test.local"));
    }

    [Test]
    public async Task CreateUser_UserManagerFails_ReturnsBadRequest()
    {
        var request = new CreateUserRequest("alice", "alice@test.local", "Alice", "Test", "weak");
        _userManager.CreateAsync(Arg.Any<ForgeUser>(), request.Password)
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Password too weak." }));

        var result = await _sut.CreateUser(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    // ─── SetActive ───────────────────────────────────────────────────────────

    [Test]
    public async Task SetActive_NonBackOffice_ReturnsForbid()
    {
        _currentUser.IsBackOffice.Returns(false);
        var result = await _sut.SetActive("id", new SetActiveRequest(false));
        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task SetActive_UserNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync("nonexistent").Returns((ForgeUser?)null);
        var result = await _sut.SetActive("nonexistent", new SetActiveRequest(false));
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task SetActive_Deactivate_SetsLockoutEnd()
    {
        var user = new ForgeUser { Id = "u1", UserName = "alice", FirstName = "Alice", LastName = "Test" };
        _userManager.FindByIdAsync("u1").Returns(user);
        _userManager.SetLockoutEnabledAsync(user, true).Returns(IdentityResult.Success);
        _userManager.SetLockoutEndDateAsync(user, Arg.Any<DateTimeOffset?>()).Returns(IdentityResult.Success);

        var result = await _sut.SetActive("u1", new SetActiveRequest(false));

        Assert.That(result, Is.TypeOf<NoContentResult>());
        await _userManager.Received(1).SetLockoutEnabledAsync(user, true);
        await _userManager.Received(1).SetLockoutEndDateAsync(user, Arg.Is<DateTimeOffset?>(d => d == DateTimeOffset.MaxValue));
    }

    [Test]
    public async Task SetActive_Activate_ClearsLockout()
    {
        var user = new ForgeUser { Id = "u1", UserName = "alice", FirstName = "Alice", LastName = "Test" };
        _userManager.FindByIdAsync("u1").Returns(user);
        _userManager.SetLockoutEndDateAsync(user, null).Returns(IdentityResult.Success);

        var result = await _sut.SetActive("u1", new SetActiveRequest(true));

        Assert.That(result, Is.TypeOf<NoContentResult>());
        await _userManager.Received(1).SetLockoutEndDateAsync(user, null);
    }

    // ─── RecordActivity ──────────────────────────────────────────────────────

    [Test]
    public async Task RecordActivity_InsertsLoginHistoryForCurrentUser()
    {
        _currentUser.UserId.Returns("user-1");
        var before = DateTime.UtcNow;

        await _sut.RecordActivity();

        var entry = Db.LoginHistory.SingleOrDefault(l => l.UserId == "user-1");
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.LoggedInAt, Is.GreaterThanOrEqualTo(before));
    }

    // ─── GetLoginHistory ─────────────────────────────────────────────────────

    [Test]
    public async Task GetLoginHistory_NonBackOffice_ReturnsForbid()
    {
        _currentUser.IsBackOffice.Returns(false);

        var result = await _sut.GetLoginHistory("user-1");

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task GetLoginHistory_BackOffice_ReturnsHistoryForUser()
    {
        var user = await SeedUser("alice");
        Db.LoginHistory.AddRange(
            new LoginHistoryEntity { UserId = user.Id, LoggedInAt = DateTime.UtcNow.AddDays(-2) },
            new LoginHistoryEntity { UserId = user.Id, LoggedInAt = DateTime.UtcNow.AddDays(-1) });
        Db.LoginHistory.Add(new LoginHistoryEntity { UserId = "other-user", LoggedInAt = DateTime.UtcNow });
        await Db.SaveChangesAsync();

        var result = await _sut.GetLoginHistory(user.Id);

        var history = result.Value!;
        Assert.That(history, Has.Count.EqualTo(2));
        Assert.That(history.All(h => h.UserId == user.Id), Is.True);
    }

    [Test]
    public async Task GetUsers_IncludesLastLoginAt_WhenUserHasHistory()
    {
        var user = await SeedUser("alice");
        var loginTime = DateTime.UtcNow.AddHours(-1);
        Db.LoginHistory.Add(new LoginHistoryEntity { UserId = user.Id, LoggedInAt = loginTime });
        await Db.SaveChangesAsync();

        var result = await _sut.GetUsers();

        var alice = result.Value!.Single(u => u.Username == "alice");
        Assert.That(alice.LastLoginAt, Is.Not.Null);
        Assert.That(alice.LastLoginAt!.Value, Is.EqualTo(loginTime).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public async Task GetUsers_LastLoginAt_NullWhenNoHistory()
    {
        await SeedUser("alice");

        var result = await _sut.GetUsers();

        var alice = result.Value!.Single(u => u.Username == "alice");
        Assert.That(alice.LastLoginAt, Is.Null);
    }
}
