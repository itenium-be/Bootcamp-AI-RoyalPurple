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
    private SkillCategoryEntity _javaCategory = null!;
    private SkillCategoryEntity _dotnetCategory = null!;

    [SetUp]
    public async Task Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _sut = new RoadmapController(Db, _user);

        var javaTeam = new TeamEntity { Name = "Java" };
        var dotnetTeam = new TeamEntity { Name = ".NET" };
        Db.Teams.AddRange(javaTeam, dotnetTeam);
        await Db.SaveChangesAsync();

        _javaCategory = new SkillCategoryEntity { Name = "Java Skills", TeamId = javaTeam.Id };
        _dotnetCategory = new SkillCategoryEntity { Name = ".NET Skills", TeamId = dotnetTeam.Id };
        Db.SkillCategories.AddRange(_javaCategory, _dotnetCategory);
        await Db.SaveChangesAsync();
    }

    [Test]
    public async Task GetRoadmap_WithoutShowAll_ReturnsOnlyTier1And2Skills()
    {
        Db.Skills.AddRange(
            new SkillEntity { Name = "Java Basics", Tier = 1, CategoryId = _javaCategory.Id },
            new SkillEntity { Name = "Spring Boot", Tier = 2, CategoryId = _javaCategory.Id },
            new SkillEntity { Name = "Microservices", Tier = 3, CategoryId = _javaCategory.Id });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { _javaCategory.TeamId!.Value });

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
            new SkillEntity { Name = "Java Basics", Tier = 1, CategoryId = _javaCategory.Id },
            new SkillEntity { Name = "Spring Boot", Tier = 2, CategoryId = _javaCategory.Id },
            new SkillEntity { Name = "Microservices", Tier = 3, CategoryId = _javaCategory.Id });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { _javaCategory.TeamId!.Value });

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
            new SkillEntity { Name = "Java Basics", Tier = 1, CategoryId = _javaCategory.Id },
            new SkillEntity { Name = "C# Fundamentals", Tier = 1, CategoryId = _dotnetCategory.Id });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { _javaCategory.TeamId!.Value });

        var result = await _sut.GetRoadmap();

        var okResult = result.Result as OkObjectResult;
        var skills = okResult!.Value as List<SkillDto>;
        Assert.That(skills, Has.Count.EqualTo(1));
        Assert.That(skills![0].Name, Is.EqualTo("Java Basics"));
    }

    [Test]
    public async Task GetRoadmap_WithNoTeams_ReturnsEmpty()
    {
        Db.Skills.Add(new SkillEntity { Name = "Java Basics", Tier = 1, CategoryId = _javaCategory.Id });
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
            Db.Skills.Add(new SkillEntity { Name = $"Foundation {i}", Tier = 1, CategoryId = _javaCategory.Id });
            Db.Skills.Add(new SkillEntity { Name = $"Core {i}", Tier = 2, CategoryId = _javaCategory.Id });
            Db.Skills.Add(new SkillEntity { Name = $"Advanced {i}", Tier = 3, CategoryId = _javaCategory.Id });
        }
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { _javaCategory.TeamId!.Value });

        var result = await _sut.GetRoadmap(showAll: false);

        var okResult = result.Result as OkObjectResult;
        var skills = okResult!.Value as List<SkillDto>;
        Assert.That(skills!.Count, Is.InRange(8, 12));
    }

    [Test]
    public async Task GetRoadmap_SkillWithPrerequisites_ReturnsPrerequisiteNames()
    {
        var basic = new SkillEntity { Name = "Java Basics", Tier = 1, CategoryId = _javaCategory.Id };
        var advanced = new SkillEntity { Name = "Spring Boot", Tier = 2, CategoryId = _javaCategory.Id };
        Db.Skills.AddRange(basic, advanced);
        await Db.SaveChangesAsync();

        Db.SkillPrerequisites.Add(new SkillPrerequisiteEntity
        {
            SkillId = advanced.Id,
            PrerequisiteSkillId = basic.Id,
        });
        await Db.SaveChangesAsync();

        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { _javaCategory.TeamId!.Value });

        var result = await _sut.GetRoadmap(showAll: true);

        var skills = (result.Result as OkObjectResult)!.Value as List<SkillDto>;
        var springBoot = skills!.Single(s => s.Name == "Spring Boot");
        Assert.That(springBoot.Prerequisites, Contains.Item("Java Basics"));
    }

    [Test]
    public async Task GetRoadmap_SkillWithoutPrerequisites_ReturnsEmptyList()
    {
        Db.Skills.Add(new SkillEntity { Name = "Java Basics", Tier = 1, CategoryId = _javaCategory.Id });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { _javaCategory.TeamId!.Value });

        var result = await _sut.GetRoadmap();

        var skills = (result.Result as OkObjectResult)!.Value as List<SkillDto>;
        Assert.That(skills![0].Prerequisites, Is.Empty);
    }

    [Test]
    public async Task GetRoadmap_SkillsOrderedByTierThenName()
    {
        Db.Skills.AddRange(
            new SkillEntity { Name = "Spring Boot", Tier = 2, CategoryId = _javaCategory.Id },
            new SkillEntity { Name = "Java Basics", Tier = 1, CategoryId = _javaCategory.Id },
            new SkillEntity { Name = "Git", Tier = 1, CategoryId = _javaCategory.Id });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { _javaCategory.TeamId!.Value });

        var result = await _sut.GetRoadmap(showAll: true);

        var okResult = result.Result as OkObjectResult;
        var skills = okResult!.Value as List<SkillDto>;
        Assert.That(skills![0].Name, Is.EqualTo("Git"));
        Assert.That(skills[1].Name, Is.EqualTo("Java Basics"));
        Assert.That(skills[2].Name, Is.EqualTo("Spring Boot"));
    }
}
