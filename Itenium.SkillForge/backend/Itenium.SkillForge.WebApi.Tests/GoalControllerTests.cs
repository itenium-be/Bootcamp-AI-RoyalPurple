using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class GoalControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private GoalController _sut = null!;
    private SkillCategoryEntity _category = null!;
    private SkillEntity _skill = null!;

    [SetUp]
    public async Task Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _sut = new GoalController(Db, _user);

        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        _category = new SkillCategoryEntity { Name = "Java Skills", TeamId = team.Id };
        Db.SkillCategories.Add(_category);
        await Db.SaveChangesAsync();

        _skill = new SkillEntity { Name = "Java Basics", Tier = 1, CategoryId = _category.Id };
        Db.Skills.Add(_skill);
        await Db.SaveChangesAsync();
    }

    [Test]
    public async Task GetGoals_ReturnsOwnGoalsForConsultant()
    {
        Db.Goals.Add(new GoalEntity
        {
            ConsultantId = "user-1",
            CoachId = "coach-1",
            SkillId = _skill.Id,
            CurrentLevel = 1,
            TargetLevel = 3,
            Deadline = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        await Db.SaveChangesAsync();

        _user.UserId.Returns("user-1");
        _user.IsManager.Returns(false);
        _user.IsBackOffice.Returns(false);

        var result = await _sut.GetGoals();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var goals = ok!.Value as IList<GoalDto>;
        Assert.That(goals, Has.Count.EqualTo(1));
        Assert.That(goals![0].SkillName, Is.EqualTo("Java Basics"));
        Assert.That(goals[0].CurrentLevel, Is.EqualTo(1));
        Assert.That(goals[0].TargetLevel, Is.EqualTo(3));
    }

    [Test]
    public async Task GetGoals_DoesNotReturnOtherConsultantsGoals()
    {
        Db.Goals.AddRange(
            new GoalEntity { ConsultantId = "user-1", CoachId = "coach-1", SkillId = _skill.Id, CurrentLevel = 1, TargetLevel = 2, Deadline = DateTime.UtcNow.AddMonths(1) },
            new GoalEntity { ConsultantId = "user-2", CoachId = "coach-1", SkillId = _skill.Id, CurrentLevel = 1, TargetLevel = 2, Deadline = DateTime.UtcNow.AddMonths(1) });
        await Db.SaveChangesAsync();

        _user.UserId.Returns("user-1");
        _user.IsManager.Returns(false);
        _user.IsBackOffice.Returns(false);

        var result = await _sut.GetGoals();

        var goals = (result.Result as OkObjectResult)!.Value as IList<GoalDto>;
        Assert.That(goals, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetGoals_IncludesResourcesOnGoal()
    {
        var goal = new GoalEntity
        {
            ConsultantId = "user-1",
            CoachId = "coach-1",
            SkillId = _skill.Id,
            CurrentLevel = 0,
            TargetLevel = 1,
            Deadline = DateTime.UtcNow.AddMonths(1),
            Resources =
            [
                new GoalResourceEntity { Title = "Clean Code", Url = "https://example.com/book", Type = "book" },
            ],
        };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();

        _user.UserId.Returns("user-1");
        _user.IsManager.Returns(false);
        _user.IsBackOffice.Returns(false);

        var result = await _sut.GetGoals();

        var goals = (result.Result as OkObjectResult)!.Value as IList<GoalDto>;
        Assert.That(goals![0].Resources, Has.Count.EqualTo(1));
        Assert.That(goals[0].Resources[0].Title, Is.EqualTo("Clean Code"));
        Assert.That(goals[0].Resources[0].Url, Is.EqualTo("https://example.com/book"));
    }

    [Test]
    public async Task CreateGoal_AsManager_CreatesGoal()
    {
        _user.UserId.Returns("coach-1");
        _user.IsManager.Returns(true);
        _user.IsBackOffice.Returns(false);

        var request = new CreateGoalRequest(
            ConsultantId: "user-1",
            SkillId: _skill.Id,
            CurrentLevel: 1,
            TargetLevel: 3,
            Deadline: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            Resources: [new CreateGoalResourceRequest("Clean Code Book", "https://example.com", "book")]);

        var result = await _sut.CreateGoal(request);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var dto = created!.Value as GoalDto;
        Assert.That(dto!.SkillName, Is.EqualTo("Java Basics"));
        Assert.That(dto.TargetLevel, Is.EqualTo(3));
        Assert.That(dto.Resources, Has.Count.EqualTo(1));

        Assert.That(Db.Goals.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetGoals_IncludesReadinessFlagRaisedAt()
    {
        var goal = new GoalEntity
        {
            ConsultantId = "user-1",
            CoachId = "coach-1",
            SkillId = _skill.Id,
            CurrentLevel = 1,
            TargetLevel = 3,
            Deadline = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();

        var raisedAt = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        Db.ReadinessFlags.Add(new ReadinessFlagEntity { GoalId = goal.Id, RaisedAt = raisedAt });
        await Db.SaveChangesAsync();

        _user.UserId.Returns("user-1");
        _user.IsManager.Returns(false);
        _user.IsBackOffice.Returns(false);

        var result = await _sut.GetGoals();

        var goals = (result.Result as OkObjectResult)!.Value as IList<GoalDto>;
        Assert.That(goals![0].ReadinessFlagRaisedAt, Is.EqualTo(raisedAt));
        Assert.That(goals[0].ReadinessFlagAgeDays, Is.EqualTo(3));
    }

    [Test]
    public async Task GetGoals_WithoutFlag_HasNullReadinessFlagRaisedAt()
    {
        Db.Goals.Add(new GoalEntity
        {
            ConsultantId = "user-1",
            CoachId = "coach-1",
            SkillId = _skill.Id,
            CurrentLevel = 1,
            TargetLevel = 3,
            Deadline = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        await Db.SaveChangesAsync();

        _user.UserId.Returns("user-1");
        _user.IsManager.Returns(false);
        _user.IsBackOffice.Returns(false);

        var result = await _sut.GetGoals();

        var goals = (result.Result as OkObjectResult)!.Value as IList<GoalDto>;
        Assert.That(goals![0].ReadinessFlagRaisedAt, Is.Null);
    }

    [Test]
    public async Task CreateGoal_AsLearner_Returns403()
    {
        _user.UserId.Returns("user-1");
        _user.IsManager.Returns(false);
        _user.IsBackOffice.Returns(false);

        var request = new CreateGoalRequest("user-1", _skill.Id, 0, 1, DateTime.UtcNow.AddMonths(1), []);

        var result = await _sut.CreateGoal(request);

        Assert.That(result.Result, Is.InstanceOf<ForbidResult>());
    }
}
