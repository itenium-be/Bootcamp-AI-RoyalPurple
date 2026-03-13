using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class TeamControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private TeamController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _sut = new TeamController(Db, _user);
    }

    [Test]
    public async Task GetUserTeams_WhenBackOffice_ReturnsAllTeams()
    {
        Db.Teams.AddRange(
            new TeamEntity { Name = "Java" },
            new TeamEntity { Name = ".NET" },
            new TeamEntity { Name = "QA" });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(true);

        var result = await _sut.GetUserTeams();

        var teams = result.Value!;
        Assert.That(teams, Has.Count.EqualTo(3));
        Assert.That(teams.Select(t => t.Name), Contains.Item("Java"));
        Assert.That(teams.Select(t => t.Name), Contains.Item(".NET"));
        Assert.That(teams.Select(t => t.Name), Contains.Item("QA"));
    }

    [Test]
    public async Task GetUserTeams_WhenNotBackOffice_ReturnsOnlyUserTeams()
    {
        var javaTeam = new TeamEntity { Name = "Java" };
        var dotnetTeam = new TeamEntity { Name = ".NET" };
        var qaTeam = new TeamEntity { Name = "QA" };
        Db.Teams.AddRange(javaTeam, dotnetTeam, qaTeam);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { javaTeam.Id, qaTeam.Id });

        var result = await _sut.GetUserTeams();

        var teams = result.Value!;
        Assert.That(teams, Has.Count.EqualTo(2));
        Assert.That(teams.Select(t => t.Name), Contains.Item("Java"));
        Assert.That(teams.Select(t => t.Name), Contains.Item("QA"));
        Assert.That(teams.Select(t => t.Name), Does.Not.Contain(".NET"));
    }

    [Test]
    public async Task GetUserTeams_WhenNotBackOfficeAndNoTeams_ReturnsEmpty()
    {
        Db.Teams.AddRange(
            new TeamEntity { Name = "Java" },
            new TeamEntity { Name = ".NET" });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(Array.Empty<int>());

        var result = await _sut.GetUserTeams();

        var teams = result.Value!;
        Assert.That(teams, Is.Empty);
    }

    [Test]
    public async Task GetUserTeams_WhenUserHasNonExistentTeamId_IgnoresIt()
    {
        var javaTeam = new TeamEntity { Name = "Java" };
        Db.Teams.Add(javaTeam);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { javaTeam.Id, 999 });

        var result = await _sut.GetUserTeams();

        var teams = result.Value!;
        Assert.That(teams, Has.Count.EqualTo(1));
        Assert.That(teams.Select(t => t.Name), Contains.Item("Java"));
    }

    [Test]
    public async Task CreateTeam_WhenBackOffice_CreatesAndReturnsCreated()
    {
        _user.IsBackOffice.Returns(true);
        var request = new CreateTeamRequest("Java");

        var result = await _sut.CreateTeam(request);

        var createdResult = result.Result as CreatedAtActionResult;
        Assert.That(createdResult, Is.Not.Null);
        var team = createdResult!.Value as TeamEntity;
        Assert.That(team!.Name, Is.EqualTo("Java"));

        var savedTeam = await Db.Teams.FindAsync(team.Id);
        Assert.That(savedTeam, Is.Not.Null);
    }

    [Test]
    public async Task CreateTeam_WhenNotBackOffice_ReturnsForbidden()
    {
        _user.IsBackOffice.Returns(false);
        var request = new CreateTeamRequest("Java");

        var result = await _sut.CreateTeam(request);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task UpdateTeam_WhenBackOffice_CanUpdateAnyTeam()
    {
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(true);
        var request = new UpdateTeamRequest("Java Champions");

        var result = await _sut.UpdateTeam(team.Id, request);

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var updated = okResult!.Value as TeamEntity;
        Assert.That(updated!.Name, Is.EqualTo("Java Champions"));
    }

    [Test]
    public async Task UpdateTeam_WhenManagerOfTeam_CanUpdateOwnTeam()
    {
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.IsManager.Returns(true);
        _user.Teams.Returns(new[] { team.Id });
        var request = new UpdateTeamRequest("Java Champions");

        var result = await _sut.UpdateTeam(team.Id, request);

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var updated = okResult!.Value as TeamEntity;
        Assert.That(updated!.Name, Is.EqualTo("Java Champions"));
    }

    [Test]
    public async Task UpdateTeam_WhenManagerOfOtherTeam_ReturnsForbidden()
    {
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.IsManager.Returns(true);
        _user.Teams.Returns(Array.Empty<int>());
        var request = new UpdateTeamRequest("Java Champions");

        var result = await _sut.UpdateTeam(team.Id, request);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task UpdateTeam_WhenLearner_ReturnsForbidden()
    {
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.IsManager.Returns(false);
        _user.Teams.Returns(new[] { team.Id });
        var request = new UpdateTeamRequest("Java Champions");

        var result = await _sut.UpdateTeam(team.Id, request);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task UpdateTeam_WhenNotFound_ReturnsNotFound()
    {
        _user.IsBackOffice.Returns(true);
        var request = new UpdateTeamRequest("Name");

        var result = await _sut.UpdateTeam(999, request);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task DeleteTeam_WhenBackOffice_DeletesAndReturnsNoContent()
    {
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(true);

        var result = await _sut.DeleteTeam(team.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var deletedTeam = await Db.Teams.FindAsync(team.Id);
        Assert.That(deletedTeam, Is.Null);
    }

    [Test]
    public async Task DeleteTeam_WhenNotBackOffice_ReturnsForbidden()
    {
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);

        var result = await _sut.DeleteTeam(team.Id);

        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task DeleteTeam_WhenNotFound_ReturnsNotFound()
    {
        _user.IsBackOffice.Returns(true);

        var result = await _sut.DeleteTeam(999);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }
}
