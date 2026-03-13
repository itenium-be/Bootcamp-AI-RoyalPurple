using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class UserControllerTests
{
    private IUserService _userService = null!;
    private ISkillForgeUser _currentUser = null!;
    private UserController _sut = null!;

    private static readonly UserDto Alice = new("id1", "alice", "alice@test.local", "Alice", "Smith", "learner", [1]);
    private static readonly UserDto Bob = new("id2", "bob", "bob@test.local", "Bob", "Jones", "manager", [1, 2]);
    private static readonly UserDto Charlie = new("id3", "charlie", "charlie@test.local", "Charlie", "Brown", "backoffice", []);
    private static readonly int[] TeamIds1 = [1];
    private static readonly int[] TeamIds12 = [1, 2];

    [SetUp]
    public void Setup()
    {
        _userService = Substitute.For<IUserService>();
        _currentUser = Substitute.For<ISkillForgeUser>();
        _sut = new UserController(_userService, _currentUser);
    }

    // GET /api/user — role-aware

    [Test]
    public async Task GetUsers_WhenBackOffice_ReturnsAllUsers()
    {
        IList<UserDto> all = [Alice, Bob, Charlie];
        _currentUser.IsBackOffice.Returns(true);
        _userService.GetAllUsersAsync().Returns(all);

        var result = await _sut.GetUsers();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value as IList<UserDto>, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetUsers_WhenManager_ReturnsTeamMembers()
    {
        IList<UserDto> teamMembers = [Alice, Bob];
        _currentUser.IsBackOffice.Returns(false);
        _currentUser.Teams.Returns(TeamIds12);
        _userService.GetTeamMembersAsync(TeamIds12).Returns(teamMembers);

        var result = await _sut.GetUsers();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value as IList<UserDto>, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetUsers_WhenLearnerWithNoTeams_ReturnsSelf()
    {
        _currentUser.IsBackOffice.Returns(false);
        _currentUser.Teams.Returns(Array.Empty<int>());
        _currentUser.UserId.Returns("id1");
        _userService.GetUserByIdAsync("id1").Returns(Alice);

        var result = await _sut.GetUsers();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var users = ok!.Value as IList<UserDto>;
        Assert.That(users, Has.Count.EqualTo(1));
        Assert.That(users![0].Id, Is.EqualTo("id1"));
    }

    [Test]
    public async Task GetUsers_WhenLearnerWithTeam_ReturnsTeamMembers()
    {
        IList<UserDto> teamMembers = [Alice];
        _currentUser.IsBackOffice.Returns(false);
        _currentUser.Teams.Returns(TeamIds1);
        _userService.GetTeamMembersAsync(TeamIds1).Returns(teamMembers);

        var result = await _sut.GetUsers();

        var ok = result.Result as OkObjectResult;
        Assert.That((ok!.Value as IList<UserDto>)![0].Id, Is.EqualTo("id1"));
    }

    // GET /api/user/me

    [Test]
    public async Task GetCurrentUser_ReturnsCurrentUserInfo()
    {
        _currentUser.UserId.Returns("id1");
        _userService.GetUserByIdAsync("id1").Returns(Alice);

        var result = await _sut.GetCurrentUser();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(Alice));
    }

    [Test]
    public async Task GetCurrentUser_WhenUserNotFound_ReturnsNotFound()
    {
        _currentUser.UserId.Returns("missing");
        _userService.GetUserByIdAsync("missing").Returns((UserDto?)null);

        var result = await _sut.GetCurrentUser();

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    // GET /api/user/coach

    [Test]
    public async Task GetCoaches_WhenUserHasTeams_ReturnsCoaches()
    {
        IList<UserDto> coaches = [Bob];
        _currentUser.Teams.Returns(TeamIds1);
        _userService.GetCoachesForTeamsAsync(TeamIds1).Returns(coaches);

        var result = await _sut.GetCoaches();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value as IList<UserDto>, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetCoaches_WhenUserHasNoTeams_ReturnsEmpty()
    {
        _currentUser.Teams.Returns(Array.Empty<int>());

        var result = await _sut.GetCoaches();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value as IList<UserDto>, Is.Empty);
    }

    // GET /api/user/{id}

    [Test]
    public async Task GetUserById_ReturnsUser()
    {
        _userService.GetUserByIdAsync("id1").Returns(Alice);

        var result = await _sut.GetUserById("id1");

        var ok = result.Result as OkObjectResult;
        Assert.That(ok!.Value, Is.EqualTo(Alice));
    }

    [Test]
    public async Task GetUserById_WhenNotFound_ReturnsNotFound()
    {
        _userService.GetUserByIdAsync("missing").Returns((UserDto?)null);

        var result = await _sut.GetUserById("missing");

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    // POST /api/user

    [Test]
    public async Task CreateUser_ReturnsCreatedUser()
    {
        var request = new CreateUserRequest("alice", "alice@test.local", "Pass123!", "Alice", "Smith", "learner", [1]);
        _userService.CreateUserAsync(request).Returns(Alice);

        var result = await _sut.CreateUser(request);

        var createdResult = result.Result as CreatedAtActionResult;
        Assert.That(createdResult, Is.Not.Null);
        Assert.That(createdResult!.Value, Is.EqualTo(Alice));
    }

    [Test]
    public async Task CreateUser_WhenServiceReturnsNull_ReturnsBadRequest()
    {
        var request = new CreateUserRequest("alice", "alice@test.local", "weak", "Alice", "Smith", "learner", []);
        _userService.CreateUserAsync(request).Returns((UserDto?)null);

        var result = await _sut.CreateUser(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestResult>());
    }

    // PUT /api/user/{id}/role

    [Test]
    public async Task AssignRole_WhenSucceeds_ReturnsNoContent()
    {
        _userService.AssignRoleAsync("id1", "manager").Returns(true);

        var result = await _sut.AssignRole("id1", new AssignRoleRequest("manager"));

        Assert.That(result, Is.TypeOf<NoContentResult>());
    }

    [Test]
    public async Task AssignRole_WhenUserNotFound_ReturnsNotFound()
    {
        _userService.AssignRoleAsync("missing", "manager").Returns(false);

        var result = await _sut.AssignRole("missing", new AssignRoleRequest("manager"));

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    // PUT /api/user/{id}/teams

    [Test]
    public async Task AssignTeams_WhenSucceeds_ReturnsNoContent()
    {
        var teams = new int[] { 1, 2 };
        _userService.AssignTeamsAsync("id1", teams).Returns(true);

        var result = await _sut.AssignTeams("id1", new AssignTeamsRequest(teams));

        Assert.That(result, Is.TypeOf<NoContentResult>());
    }

    [Test]
    public async Task AssignTeams_WhenUserNotFound_ReturnsNotFound()
    {
        var teams = Array.Empty<int>();
        _userService.AssignTeamsAsync("missing", teams).Returns(false);

        var result = await _sut.AssignTeams("missing", new AssignTeamsRequest(teams));

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }
}
