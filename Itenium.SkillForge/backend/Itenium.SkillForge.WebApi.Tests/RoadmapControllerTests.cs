using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class RoadmapControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private RoadmapController _sut = null!;
    private TeamEntity _javaTeam = null!;
    private TeamEntity _dotnetTeam = null!;

    [SetUp]
    public async Task Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _sut = new RoadmapController(Db, _user);

        _javaTeam = new TeamEntity { Name = "Java" };
        _dotnetTeam = new TeamEntity { Name = ".NET" };
        Db.Teams.AddRange(_javaTeam, _dotnetTeam);
        await Db.SaveChangesAsync();
    }

    [Test]
    public async Task GetRoadmap_WithoutShowAll_ReturnsOnlyTier1And2Skills()
    {
        Db.Skills.AddRange(
            new SkillEntity { Name = "Java Basics", Tier = 1, TeamId = _javaTeam.Id },
            new SkillEntity { Name = "Spring Boot", Tier = 2, TeamId = _javaTeam.Id },
            new SkillEntity { Name = "Microservices", Tier = 3, TeamId = _javaTeam.Id });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { _javaTeam.Id });

        var result = await _sut.GetRoadmap(showAll: false);

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var skills = okResult!.Value as List<SkillDto>;
        Assert.That(skills, Has.Count.EqualTo(2));
        Assert.That(skills!.Select(s => s.Name), Does.Not.Contain("Microservices"));
    }

    [Test]
    public async Task GetRoadmap_WithShowAll_ReturnsAllTiers()
    {
        Db.Skills.AddRange(
            new SkillEntity { Name = "Java Basics", Tier = 1, TeamId = _javaTeam.Id },
            new SkillEntity { Name = "Spring Boot", Tier = 2, TeamId = _javaTeam.Id },
            new SkillEntity { Name = "Microservices", Tier = 3, TeamId = _javaTeam.Id });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { _javaTeam.Id });

        var result = await _sut.GetRoadmap(showAll: true);

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var skills = okResult!.Value as List<SkillDto>;
        Assert.That(skills, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetRoadmap_FiltersToUserTeam()
    {
        Db.Skills.AddRange(
            new SkillEntity { Name = "Java Basics", Tier = 1, TeamId = _javaTeam.Id },
            new SkillEntity { Name = "C# Fundamentals", Tier = 1, TeamId = _dotnetTeam.Id });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { _javaTeam.Id });

        var result = await _sut.GetRoadmap();

        var okResult = result.Result as OkObjectResult;
        var skills = okResult!.Value as List<SkillDto>;
        Assert.That(skills, Has.Count.EqualTo(1));
        Assert.That(skills![0].Name, Is.EqualTo("Java Basics"));
    }

    [Test]
    public async Task GetRoadmap_WithNoTeams_ReturnsEmpty()
    {
        Db.Skills.Add(new SkillEntity { Name = "Java Basics", Tier = 1, TeamId = _javaTeam.Id });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(Array.Empty<int>());

        var result = await _sut.GetRoadmap();

        var okResult = result.Result as OkObjectResult;
        var skills = okResult!.Value as List<SkillDto>;
        Assert.That(skills, Is.Empty);
    }

    [Test]
    public async Task GetRoadmap_DefaultView_ReturnsBetween8And12Nodes()
    {
        for (var i = 1; i <= 5; i++)
        {
            Db.Skills.Add(new SkillEntity { Name = $"Foundation {i}", Tier = 1, TeamId = _javaTeam.Id });
            Db.Skills.Add(new SkillEntity { Name = $"Core {i}", Tier = 2, TeamId = _javaTeam.Id });
            Db.Skills.Add(new SkillEntity { Name = $"Advanced {i}", Tier = 3, TeamId = _javaTeam.Id });
        }
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { _javaTeam.Id });

        var result = await _sut.GetRoadmap(showAll: false);

        var okResult = result.Result as OkObjectResult;
        var skills = okResult!.Value as List<SkillDto>;
        Assert.That(skills!.Count, Is.InRange(8, 12));
    }

    [Test]
    public async Task GetRoadmap_SkillsOrderedByTierThenName()
    {
        Db.Skills.AddRange(
            new SkillEntity { Name = "Spring Boot", Tier = 2, TeamId = _javaTeam.Id },
            new SkillEntity { Name = "Java Basics", Tier = 1, TeamId = _javaTeam.Id },
            new SkillEntity { Name = "Git", Tier = 1, TeamId = _javaTeam.Id });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { _javaTeam.Id });

        var result = await _sut.GetRoadmap(showAll: true);

        var okResult = result.Result as OkObjectResult;
        var skills = okResult!.Value as List<SkillDto>;
        Assert.That(skills![0].Name, Is.EqualTo("Git"));
        Assert.That(skills[1].Name, Is.EqualTo("Java Basics"));
        Assert.That(skills[2].Name, Is.EqualTo("Spring Boot"));
    }
}
