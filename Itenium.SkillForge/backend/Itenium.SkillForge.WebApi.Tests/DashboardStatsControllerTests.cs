using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class DashboardStatsControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private IDashboardService _dashboardService = null!;
    private DashboardController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _dashboardService = Substitute.For<IDashboardService>();
        _sut = new DashboardController(_dashboardService, _user, Db);
    }

    [Test]
    public async Task GetStats_ReturnsTotalCourseCount()
    {
        Db.Courses.AddRange(
            new CourseEntity { Name = "C# Basics" },
            new CourseEntity { Name = "React Intro" });
        await Db.SaveChangesAsync();

        var result = await _sut.GetStats();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var stats = ok!.Value as DashboardStatsDto;
        Assert.That(stats!.TotalCourses, Is.EqualTo(2));
    }

    [Test]
    public async Task GetStats_ReturnsActiveLearners_FromManagerTeams()
    {
        var team1 = new TeamEntity { Name = ".NET" };
        var team2 = new TeamEntity { Name = "Java" };
        Db.Teams.AddRange(team1, team2);
        await Db.SaveChangesAsync();

        Db.ConsultantProfiles.AddRange(
            new ConsultantProfileEntity { UserId = "user-1", TeamId = team1.Id },
            new ConsultantProfileEntity { UserId = "user-2", TeamId = team1.Id },
            new ConsultantProfileEntity { UserId = "user-3", TeamId = team2.Id });
        await Db.SaveChangesAsync();

        _user.Teams.Returns([team1.Id]);

        var result = await _sut.GetStats();

        var ok = result.Result as OkObjectResult;
        var stats = ok!.Value as DashboardStatsDto;
        Assert.That(stats!.ActiveLearners, Is.EqualTo(2));
    }

    [Test]
    public async Task GetStats_ReturnsAssignedCourses_ForManagerTeams()
    {
        var team = new TeamEntity { Name = ".NET" };
        var course = new CourseEntity { Name = "C# Basics" };
        Db.Teams.Add(team);
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();

        Db.CourseAssignments.AddRange(
            new CourseAssignmentEntity { CourseId = course.Id, TeamId = team.Id, IsRequired = true, AssignedById = "coach" },
            new CourseAssignmentEntity { CourseId = course.Id, TeamId = team.Id, IsRequired = false, AssignedById = "coach" });
        await Db.SaveChangesAsync();

        _user.Teams.Returns([team.Id]);

        var result = await _sut.GetStats();

        var ok = result.Result as OkObjectResult;
        var stats = ok!.Value as DashboardStatsDto;
        Assert.That(stats!.AssignedCourses, Is.EqualTo(2));
    }

    [Test]
    public async Task GetStats_WhenNoCourses_ReturnsZero()
    {
        _user.Teams.Returns([]);

        var result = await _sut.GetStats();

        var ok = result.Result as OkObjectResult;
        var stats = ok!.Value as DashboardStatsDto;
        Assert.That(stats!.TotalCourses, Is.EqualTo(0));
        Assert.That(stats.ActiveLearners, Is.EqualTo(0));
        Assert.That(stats.AssignedCourses, Is.EqualTo(0));
    }
}
