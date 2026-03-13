using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class ConsultantProfileControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private IUserService _userService = null!;
    private ConsultantProfileController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _userService = Substitute.For<IUserService>();
        _sut = new ConsultantProfileController(Db, _user, _userService);
    }

    [Test]
    public async Task GetConsultants_ReturnsAllConsultantsWithProfiles()
    {
        var team = new TeamEntity { Name = ".NET" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        Db.ConsultantProfiles.Add(new ConsultantProfileEntity
        {
            UserId = "user-1",
            TeamId = team.Id,
        });
        await Db.SaveChangesAsync();

        _userService.GetAllUsersAsync().Returns([
            new UserDto("user-1", "jdoe", "j@example.com", "John", "Doe", "learner", []),
        ]);

        var result = await _sut.GetConsultants();

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var consultants = okResult!.Value as List<ConsultantDto>;
        Assert.That(consultants, Has.Count.EqualTo(1));
        Assert.That(consultants![0].UserId, Is.EqualTo("user-1"));
        Assert.That(consultants[0].TeamId, Is.EqualTo(team.Id));
        Assert.That(consultants[0].TeamName, Is.EqualTo(".NET"));
        Assert.That(consultants[0].FirstName, Is.EqualTo("John"));
        Assert.That(consultants[0].LastName, Is.EqualTo("Doe"));
    }

    [Test]
    public async Task GetConsultants_WhenNoProfiles_ReturnsEmptyList()
    {
        _userService.GetAllUsersAsync().Returns([]);

        var result = await _sut.GetConsultants();

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var consultants = okResult!.Value as List<ConsultantDto>;
        Assert.That(consultants, Is.Empty);
    }

    [Test]
    public async Task AssignProfile_CreatesNewAssignment()
    {
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        var request = new AssignProfileRequest(team.Id);
        var result = await _sut.AssignProfile("user-abc", request);

        Assert.That(result, Is.TypeOf<OkResult>());
        var saved = Db.ConsultantProfiles.FirstOrDefault(p => p.UserId == "user-abc");
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.TeamId, Is.EqualTo(team.Id));
    }

    [Test]
    public async Task AssignProfile_WhenAlreadyAssigned_UpdatesExistingAssignment()
    {
        var team1 = new TeamEntity { Name = "Java" };
        var team2 = new TeamEntity { Name = ".NET" };
        Db.Teams.AddRange(team1, team2);
        await Db.SaveChangesAsync();

        Db.ConsultantProfiles.Add(new ConsultantProfileEntity { UserId = "user-abc", TeamId = team1.Id });
        await Db.SaveChangesAsync();

        var request = new AssignProfileRequest(team2.Id);
        var result = await _sut.AssignProfile("user-abc", request);

        Assert.That(result, Is.TypeOf<OkResult>());
        var profiles = Db.ConsultantProfiles.Where(p => p.UserId == "user-abc").ToList();
        Assert.That(profiles, Has.Count.EqualTo(1));
        Assert.That(profiles[0].TeamId, Is.EqualTo(team2.Id));
    }

    [Test]
    public async Task AssignProfile_WhenTeamNotFound_ReturnsNotFound()
    {
        var request = new AssignProfileRequest(999);
        var result = await _sut.AssignProfile("user-abc", request);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task RemoveProfile_WhenExists_DeletesAssignment()
    {
        var team = new TeamEntity { Name = "QA" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        Db.ConsultantProfiles.Add(new ConsultantProfileEntity { UserId = "user-abc", TeamId = team.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.RemoveProfile("user-abc");

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var saved = Db.ConsultantProfiles.FirstOrDefault(p => p.UserId == "user-abc");
        Assert.That(saved, Is.Null);
    }

    [Test]
    public async Task RemoveProfile_WhenNotExists_ReturnsNotFound()
    {
        var result = await _sut.RemoveProfile("non-existent-user");

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task GetMyProfile_WhenAssigned_ReturnsProfile()
    {
        var team = new TeamEntity { Name = ".NET" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        Db.ConsultantProfiles.Add(new ConsultantProfileEntity { UserId = "current-user", TeamId = team.Id });
        await Db.SaveChangesAsync();

        _user.UserId.Returns("current-user");
        _userService.GetUserByIdAsync("current-user").Returns(
            new UserDto("current-user", "jdoe", "j@example.com", "John", "Doe", "learner", []));

        var result = await _sut.GetMyProfile();

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var profile = okResult!.Value as ConsultantDto;
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.TeamName, Is.EqualTo(".NET"));
        Assert.That(profile.FirstName, Is.EqualTo("John"));
        Assert.That(profile.LastName, Is.EqualTo("Doe"));
    }

    [Test]
    public async Task GetMyProfile_WhenNotAssigned_ReturnsNotFound()
    {
        _user.UserId.Returns("current-user");

        var result = await _sut.GetMyProfile();

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }
}
