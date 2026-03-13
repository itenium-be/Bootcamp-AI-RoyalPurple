using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class ReadinessFlagControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private ReadinessFlagController _sut = null!;
    private GoalEntity _goal = null!;

    [SetUp]
    public async Task Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _sut = new ReadinessFlagController(Db, _user);

        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        var category = new SkillCategoryEntity { Name = "Java Skills", TeamId = team.Id };
        Db.SkillCategories.Add(category);
        await Db.SaveChangesAsync();

        var skill = new SkillEntity { Name = "Java Basics", Tier = 1, CategoryId = category.Id };
        Db.Skills.Add(skill);
        await Db.SaveChangesAsync();

        _goal = new GoalEntity
        {
            ConsultantId = "user-1",
            CoachId = "coach-1",
            SkillId = skill.Id,
            CurrentLevel = 1,
            TargetLevel = 3,
            Deadline = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        Db.Goals.Add(_goal);
        await Db.SaveChangesAsync();
    }

    [Test]
    public async Task RaiseFlag_CreatesFlag()
    {
        _user.UserId.Returns("user-1");
        _user.IsManager.Returns(false);
        _user.IsBackOffice.Returns(false);

        var result = await _sut.RaiseFlag(_goal.Id);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(Db.ReadinessFlags.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task RaiseFlag_AlreadyFlagged_UpdatesRaisedAt()
    {
        var earlier = DateTime.UtcNow.AddDays(-5);
        Db.ReadinessFlags.Add(new ReadinessFlagEntity { GoalId = _goal.Id, RaisedAt = earlier });
        await Db.SaveChangesAsync();

        _user.UserId.Returns("user-1");
        _user.IsManager.Returns(false);
        _user.IsBackOffice.Returns(false);

        await _sut.RaiseFlag(_goal.Id);

        var flag = Db.ReadinessFlags.Single();
        Assert.That(flag.RaisedAt, Is.GreaterThan(earlier));
    }

    [Test]
    public async Task RaiseFlag_OnOtherConsultantsGoal_Returns403()
    {
        _user.UserId.Returns("user-2");
        _user.IsManager.Returns(false);
        _user.IsBackOffice.Returns(false);

        var result = await _sut.RaiseFlag(_goal.Id);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task LowerFlag_RemovesFlag()
    {
        Db.ReadinessFlags.Add(new ReadinessFlagEntity { GoalId = _goal.Id, RaisedAt = DateTime.UtcNow });
        await Db.SaveChangesAsync();

        _user.UserId.Returns("user-1");
        _user.IsManager.Returns(false);
        _user.IsBackOffice.Returns(false);

        var result = await _sut.LowerFlag(_goal.Id);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(Db.ReadinessFlags.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task LowerFlag_WhenNoFlag_Returns404()
    {
        _user.UserId.Returns("user-1");
        _user.IsManager.Returns(false);
        _user.IsBackOffice.Returns(false);

        var result = await _sut.LowerFlag(_goal.Id);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetFlags_AsManager_ReturnsFlags()
    {
        var raisedAt = DateTime.UtcNow.AddDays(-3);
        Db.ReadinessFlags.Add(new ReadinessFlagEntity { GoalId = _goal.Id, RaisedAt = raisedAt });
        await Db.SaveChangesAsync();

        _user.IsManager.Returns(true);
        _user.IsBackOffice.Returns(false);

        var result = await _sut.GetFlags();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var flags = ok!.Value as IList<ReadinessFlagDto>;
        Assert.That(flags, Has.Count.EqualTo(1));
        Assert.That(flags![0].SkillName, Is.EqualTo("Java Basics"));
        Assert.That(flags[0].ConsultantId, Is.EqualTo("user-1"));
        Assert.That(flags[0].AgeDays, Is.EqualTo(3));
    }

    [Test]
    public async Task GetFlags_AsConsultant_Returns403()
    {
        _user.IsManager.Returns(false);
        _user.IsBackOffice.Returns(false);

        var result = await _sut.GetFlags();

        Assert.That(result.Result, Is.InstanceOf<ForbidResult>());
    }
}
