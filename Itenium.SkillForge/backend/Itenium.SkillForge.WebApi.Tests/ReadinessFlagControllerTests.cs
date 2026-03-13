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
    private GoalController _sut = null!;
    private GoalEntity _ownGoal = null!;
    private GoalEntity _otherGoal = null!;

    [SetUp]
    public async Task Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _sut = new GoalController(Db, _user);

        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        var category = new SkillCategoryEntity { Name = "Java Skills", TeamId = team.Id };
        Db.SkillCategories.Add(category);
        await Db.SaveChangesAsync();

        var skill = new SkillEntity { Name = "Java Basics", Tier = 1, CategoryId = category.Id };
        Db.Skills.Add(skill);
        await Db.SaveChangesAsync();

        _ownGoal = new GoalEntity
        {
            ConsultantId = "learner-1",
            CoachId = "coach-1",
            SkillId = skill.Id,
            CurrentLevel = 1,
            TargetLevel = 3,
            Deadline = DateTime.UtcNow.AddMonths(3),
        };
        _otherGoal = new GoalEntity
        {
            ConsultantId = "learner-2",
            CoachId = "coach-1",
            SkillId = skill.Id,
            CurrentLevel = 1,
            TargetLevel = 3,
            Deadline = DateTime.UtcNow.AddMonths(3),
        };
        Db.Goals.AddRange(_ownGoal, _otherGoal);
        await Db.SaveChangesAsync();

        _user.UserId.Returns("learner-1");
        _user.IsManager.Returns(false);
        _user.IsBackOffice.Returns(false);
    }

    // POST /api/goal/{goalId}/readiness-flag

    [Test]
    public async Task RaiseFlag_AsLearner_OnOwnGoal_Returns204()
    {
        var result = await _sut.RaiseReadinessFlag(_ownGoal.Id);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(Db.ReadinessFlags.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task RaiseFlag_OnOtherConsultantGoal_Returns403()
    {
        var result = await _sut.RaiseReadinessFlag(_otherGoal.Id);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task RaiseFlag_WhenAlreadyActive_Returns409()
    {
        Db.ReadinessFlags.Add(new ReadinessFlagEntity
        {
            GoalId = _ownGoal.Id,
            RaisedAt = DateTime.UtcNow.AddDays(-1),
        });
        await Db.SaveChangesAsync();

        var result = await _sut.RaiseReadinessFlag(_ownGoal.Id);

        Assert.That(result, Is.InstanceOf<ConflictResult>());
    }

    [Test]
    public async Task RaiseFlag_OnNonExistentGoal_Returns404()
    {
        var result = await _sut.RaiseReadinessFlag(99999);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    // DELETE /api/goal/{goalId}/readiness-flag

    [Test]
    public async Task ResolveFlag_AsLearner_OnOwnGoal_Returns204()
    {
        Db.ReadinessFlags.Add(new ReadinessFlagEntity
        {
            GoalId = _ownGoal.Id,
            RaisedAt = DateTime.UtcNow.AddDays(-2),
        });
        await Db.SaveChangesAsync();

        var result = await _sut.ResolveReadinessFlag(_ownGoal.Id);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(Db.ReadinessFlags.Single().ResolvedAt, Is.Not.Null);
    }

    [Test]
    public async Task ResolveFlag_WhenNoActiveFlag_Returns404()
    {
        var result = await _sut.ResolveReadinessFlag(_ownGoal.Id);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task ResolveFlag_AsManager_OnConsultantGoal_Returns204()
    {
        _user.UserId.Returns("coach-1");
        _user.IsManager.Returns(true);

        Db.ReadinessFlags.Add(new ReadinessFlagEntity
        {
            GoalId = _ownGoal.Id,
            RaisedAt = DateTime.UtcNow.AddDays(-2),
        });
        await Db.SaveChangesAsync();

        var result = await _sut.ResolveReadinessFlag(_ownGoal.Id);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task ResolveFlag_AsManager_OnOtherTeamsGoal_Returns403()
    {
        _user.UserId.Returns("other-coach");
        _user.IsManager.Returns(true);

        Db.ReadinessFlags.Add(new ReadinessFlagEntity
        {
            GoalId = _ownGoal.Id,
            RaisedAt = DateTime.UtcNow.AddDays(-2),
        });
        await Db.SaveChangesAsync();

        var result = await _sut.ResolveReadinessFlag(_ownGoal.Id);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }
}
