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

    private const string CoachId = "coach-001";
    private const string ConsultantId = "learner-001";
    private const string OtherConsultantId = "learner-002";

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _sut = new GoalController(Db, _user);
    }

    // --- GET /api/goal ---

    [Test]
    public async Task GetGoals_AsConsultant_ReturnsOwnActiveGoalsOnly()
    {
        Db.Goals.AddRange(
            new GoalEntity { SkillName = "C# Basics", ConsultantUserId = ConsultantId, CreatedByCoachId = CoachId, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(1) },
            new GoalEntity { SkillName = "Docker", ConsultantUserId = OtherConsultantId, CreatedByCoachId = CoachId, TargetNiveau = 1, Deadline = DateTime.UtcNow.AddMonths(1) });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.IsManager.Returns(false);
        _user.UserId.Returns(ConsultantId);

        var result = await _sut.GetGoals();

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var goals = okResult!.Value as List<GoalEntity>;
        Assert.That(goals, Has.Count.EqualTo(1));
        Assert.That(goals![0].SkillName, Is.EqualTo("C# Basics"));
    }

    [Test]
    public async Task GetGoals_AsManager_ReturnsAllGoals()
    {
        Db.Goals.AddRange(
            new GoalEntity { SkillName = "C# Basics", ConsultantUserId = ConsultantId, CreatedByCoachId = CoachId, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(1) },
            new GoalEntity { SkillName = "Docker", ConsultantUserId = OtherConsultantId, CreatedByCoachId = CoachId, TargetNiveau = 1, Deadline = DateTime.UtcNow.AddMonths(1) });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.IsManager.Returns(true);
        _user.UserId.Returns(CoachId);

        var result = await _sut.GetGoals();

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var goals = okResult!.Value as List<GoalEntity>;
        Assert.That(goals, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetGoals_WhenNoGoals_ReturnsEmptyList()
    {
        _user.IsBackOffice.Returns(false);
        _user.IsManager.Returns(false);
        _user.UserId.Returns(ConsultantId);

        var result = await _sut.GetGoals();

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var goals = okResult!.Value as List<GoalEntity>;
        Assert.That(goals, Is.Empty);
    }

    // --- GET /api/goal/{id} ---

    [Test]
    public async Task GetGoal_AsConsultant_WhenOwnGoal_ReturnsGoal()
    {
        var goal = new GoalEntity { SkillName = "REST API", ConsultantUserId = ConsultantId, CreatedByCoachId = CoachId, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(2) };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.IsManager.Returns(false);
        _user.UserId.Returns(ConsultantId);

        var result = await _sut.GetGoal(goal.Id);

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var returnedGoal = okResult!.Value as GoalEntity;
        Assert.That(returnedGoal!.SkillName, Is.EqualTo("REST API"));
    }

    [Test]
    public async Task GetGoal_AsConsultant_WhenOtherConsultantsGoal_ReturnsForbid()
    {
        var goal = new GoalEntity { SkillName = "Docker", ConsultantUserId = OtherConsultantId, CreatedByCoachId = CoachId, TargetNiveau = 1, Deadline = DateTime.UtcNow.AddMonths(1) };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.IsManager.Returns(false);
        _user.UserId.Returns(ConsultantId);

        var result = await _sut.GetGoal(goal.Id);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task GetGoal_WhenNotExists_ReturnsNotFound()
    {
        _user.IsBackOffice.Returns(false);
        _user.IsManager.Returns(true);
        _user.UserId.Returns(CoachId);

        var result = await _sut.GetGoal(999);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    // --- POST /api/goal ---

    [Test]
    public async Task CreateGoal_AsManager_CreatesAndReturnsCreated()
    {
        _user.IsManager.Returns(true);
        _user.UserId.Returns(CoachId);
        var request = new CreateGoalRequest(ConsultantId, "Clean Code", 1, 3, DateTime.UtcNow.AddMonths(3));

        var result = await _sut.CreateGoal(request);

        var createdResult = result.Result as CreatedAtActionResult;
        Assert.That(createdResult, Is.Not.Null);
        var goal = createdResult!.Value as GoalEntity;
        Assert.That(goal!.SkillName, Is.EqualTo("Clean Code"));
        Assert.That(goal.ConsultantUserId, Is.EqualTo(ConsultantId));
        Assert.That(goal.CreatedByCoachId, Is.EqualTo(CoachId));
        Assert.That(goal.CurrentNiveau, Is.EqualTo(1));
        Assert.That(goal.TargetNiveau, Is.EqualTo(3));
        Assert.That(goal.IsActive, Is.True);
        Assert.That(goal.ReadinessFlagRaisedAt, Is.Null);

        var savedGoal = await Db.Goals.FindAsync(goal.Id);
        Assert.That(savedGoal, Is.Not.Null);
    }

    [Test]
    public async Task CreateGoal_AsConsultant_ReturnsForbid()
    {
        _user.IsManager.Returns(false);
        _user.UserId.Returns(ConsultantId);
        var request = new CreateGoalRequest(ConsultantId, "Clean Code", 1, 3, DateTime.UtcNow.AddMonths(3));

        var result = await _sut.CreateGoal(request);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    // --- PUT /api/goal/{id} ---

    [Test]
    public async Task UpdateGoal_AsManager_WhenExists_UpdatesAndReturnsOk()
    {
        var goal = new GoalEntity { SkillName = "Old Skill", ConsultantUserId = ConsultantId, CreatedByCoachId = CoachId, CurrentNiveau = 1, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(1) };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();
        _user.IsManager.Returns(true);
        _user.UserId.Returns(CoachId);
        var newDeadline = DateTime.UtcNow.AddMonths(3);
        var request = new UpdateGoalRequest("New Skill", 2, 4, newDeadline);

        var result = await _sut.UpdateGoal(goal.Id, request);

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var updated = okResult!.Value as GoalEntity;
        Assert.That(updated!.SkillName, Is.EqualTo("New Skill"));
        Assert.That(updated.CurrentNiveau, Is.EqualTo(2));
        Assert.That(updated.TargetNiveau, Is.EqualTo(4));
    }

    [Test]
    public async Task UpdateGoal_AsConsultant_ReturnsForbid()
    {
        var goal = new GoalEntity { SkillName = "Skill", ConsultantUserId = ConsultantId, CreatedByCoachId = CoachId, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(1) };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();
        _user.IsManager.Returns(false);
        _user.UserId.Returns(ConsultantId);

        var result = await _sut.UpdateGoal(goal.Id, new UpdateGoalRequest("X", 1, 2, DateTime.UtcNow));

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task UpdateGoal_WhenNotExists_ReturnsNotFound()
    {
        _user.IsManager.Returns(true);
        _user.UserId.Returns(CoachId);

        var result = await _sut.UpdateGoal(999, new UpdateGoalRequest("X", 1, 2, DateTime.UtcNow));

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    // --- DELETE /api/goal/{id} ---

    [Test]
    public async Task DeleteGoal_AsManager_WhenExists_RemovesAndReturnsNoContent()
    {
        var goal = new GoalEntity { SkillName = "Skill", ConsultantUserId = ConsultantId, CreatedByCoachId = CoachId, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(1) };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();
        _user.IsManager.Returns(true);

        var result = await _sut.DeleteGoal(goal.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(await Db.Goals.FindAsync(goal.Id), Is.Null);
    }

    [Test]
    public async Task DeleteGoal_AsConsultant_ReturnsForbid()
    {
        var goal = new GoalEntity { SkillName = "Skill", ConsultantUserId = ConsultantId, CreatedByCoachId = CoachId, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(1) };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();
        _user.IsManager.Returns(false);

        var result = await _sut.DeleteGoal(goal.Id);

        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task DeleteGoal_WhenNotExists_ReturnsNotFound()
    {
        _user.IsManager.Returns(true);

        var result = await _sut.DeleteGoal(999);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    // --- POST /api/goal/{id}/readiness ---

    [Test]
    public async Task RaiseReadinessFlag_WhenOwnGoalAndNoExistingFlag_SetsFlag()
    {
        var goal = new GoalEntity { SkillName = "Skill", ConsultantUserId = ConsultantId, CreatedByCoachId = CoachId, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(1) };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();
        _user.IsManager.Returns(false);
        _user.UserId.Returns(ConsultantId);

        var result = await _sut.RaiseReadinessFlag(goal.Id);

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var updated = okResult!.Value as GoalEntity;
        Assert.That(updated!.ReadinessFlagRaisedAt, Is.Not.Null);

        var savedGoal = await Db.Goals.FindAsync(goal.Id);
        Assert.That(savedGoal!.ReadinessFlagRaisedAt, Is.Not.Null);
    }

    [Test]
    public async Task RaiseReadinessFlag_WhenAlreadyRaised_ReturnsBadRequest()
    {
        var goal = new GoalEntity { SkillName = "Skill", ConsultantUserId = ConsultantId, CreatedByCoachId = CoachId, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(1), ReadinessFlagRaisedAt = DateTime.UtcNow.AddDays(-1) };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();
        _user.IsManager.Returns(false);
        _user.UserId.Returns(ConsultantId);

        var result = await _sut.RaiseReadinessFlag(goal.Id);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task RaiseReadinessFlag_WhenOtherConsultantsGoal_ReturnsForbid()
    {
        var goal = new GoalEntity { SkillName = "Skill", ConsultantUserId = OtherConsultantId, CreatedByCoachId = CoachId, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(1) };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();
        _user.IsManager.Returns(false);
        _user.UserId.Returns(ConsultantId);

        var result = await _sut.RaiseReadinessFlag(goal.Id);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    // --- DELETE /api/goal/{id}/readiness ---

    [Test]
    public async Task ClearReadinessFlag_AsManager_WhenFlagExists_ClearsFlag()
    {
        var goal = new GoalEntity { SkillName = "Skill", ConsultantUserId = ConsultantId, CreatedByCoachId = CoachId, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(1), ReadinessFlagRaisedAt = DateTime.UtcNow.AddDays(-2) };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();
        _user.IsManager.Returns(true);

        var result = await _sut.ClearReadinessFlag(goal.Id);

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var updated = okResult!.Value as GoalEntity;
        Assert.That(updated!.ReadinessFlagRaisedAt, Is.Null);
    }

    [Test]
    public async Task ClearReadinessFlag_AsConsultant_ReturnsForbid()
    {
        var goal = new GoalEntity { SkillName = "Skill", ConsultantUserId = ConsultantId, CreatedByCoachId = CoachId, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(1), ReadinessFlagRaisedAt = DateTime.UtcNow };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();
        _user.IsManager.Returns(false);

        var result = await _sut.ClearReadinessFlag(goal.Id);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task ClearReadinessFlag_WhenNotExists_ReturnsNotFound()
    {
        _user.IsManager.Returns(true);

        var result = await _sut.ClearReadinessFlag(999);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    // --- LinkedResources (FR16) ---

    [Test]
    public async Task CreateGoal_WithLinkedResources_PersistsLinkedResources()
    {
        _user.IsManager.Returns(true);
        _user.UserId.Returns(CoachId);
        var resources = "https://example.com/course1\nhttps://example.com/course2";
        var request = new CreateGoalRequest(ConsultantId, "Clean Code", 1, 3, DateTime.UtcNow.AddMonths(3), resources);

        var result = await _sut.CreateGoal(request);

        var createdResult = result.Result as CreatedAtActionResult;
        Assert.That(createdResult, Is.Not.Null);
        var goal = createdResult!.Value as GoalEntity;
        Assert.That(goal!.LinkedResources, Is.EqualTo(resources));
    }

    [Test]
    public async Task UpdateGoal_WithLinkedResources_UpdatesLinkedResources()
    {
        var goal = new GoalEntity { SkillName = "Skill", ConsultantUserId = ConsultantId, CreatedByCoachId = CoachId, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(1) };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();
        _user.IsManager.Returns(true);
        _user.UserId.Returns(CoachId);
        var resources = "https://example.com/resource";
        var request = new UpdateGoalRequest("Skill", 1, 2, DateTime.UtcNow.AddMonths(1), resources);

        var result = await _sut.UpdateGoal(goal.Id, request);

        var okResult = result.Result as OkObjectResult;
        var updated = okResult!.Value as GoalEntity;
        Assert.That(updated!.LinkedResources, Is.EqualTo(resources));
    }

    // --- Flag age (FR19) ---

    [Test]
    public async Task GetGoals_ReadinessFlagAge_IsTrackedByRaisedAtTimestamp()
    {
        var raisedAt = DateTime.UtcNow.AddDays(-3);
        var goal = new GoalEntity { SkillName = "Skill", ConsultantUserId = ConsultantId, CreatedByCoachId = CoachId, TargetNiveau = 2, Deadline = DateTime.UtcNow.AddMonths(1), ReadinessFlagRaisedAt = raisedAt };
        Db.Goals.Add(goal);
        await Db.SaveChangesAsync();
        _user.IsManager.Returns(true);

        var result = await _sut.GetGoals();

        var okResult = result.Result as OkObjectResult;
        var goals = okResult!.Value as List<GoalEntity>;
        var returnedGoal = goals![0];
        Assert.That(returnedGoal.ReadinessFlagRaisedAt, Is.EqualTo(raisedAt).Within(TimeSpan.FromSeconds(1)));
        Assert.That((DateTime.UtcNow - returnedGoal.ReadinessFlagRaisedAt!.Value).Days, Is.EqualTo(3));
    }
}
