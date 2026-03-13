using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class AssignmentControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private AssignmentController _sut = null!;

    private TeamEntity _team = null!;
    private CourseEntity _course = null!;

    [SetUp]
    public async Task Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _sut = new AssignmentController(Db, _user);

        _team = new TeamEntity { Name = ".NET" };
        _course = new CourseEntity { Name = "C# Basics" };
        Db.Teams.Add(_team);
        Db.Courses.Add(_course);
        await Db.SaveChangesAsync();
    }

    // GET

    [Test]
    public async Task GetAssignments_ReturnsAssignmentsForManagerTeams()
    {
        _user.Teams.Returns([_team.Id]);

        Db.CourseAssignments.Add(new CourseAssignmentEntity
        {
            CourseId = _course.Id,
            TeamId = _team.Id,
            IsRequired = true,
            AssignedById = "coach-1",
        });
        await Db.SaveChangesAsync();

        var result = await _sut.GetAssignments();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var assignments = ok!.Value as List<AssignmentDto>;
        Assert.That(assignments, Has.Count.EqualTo(1));
        Assert.That(assignments![0].CourseId, Is.EqualTo(_course.Id));
        Assert.That(assignments[0].TeamId, Is.EqualTo(_team.Id));
        Assert.That(assignments[0].IsRequired, Is.True);
    }

    [Test]
    public async Task GetAssignments_DoesNotReturnAssignmentsForOtherTeams()
    {
        var otherTeam = new TeamEntity { Name = "Java" };
        Db.Teams.Add(otherTeam);
        await Db.SaveChangesAsync();

        _user.Teams.Returns([_team.Id]);

        Db.CourseAssignments.Add(new CourseAssignmentEntity
        {
            CourseId = _course.Id,
            TeamId = otherTeam.Id,
            IsRequired = false,
            AssignedById = "coach-2",
        });
        await Db.SaveChangesAsync();

        var result = await _sut.GetAssignments();

        var ok = result.Result as OkObjectResult;
        var assignments = ok!.Value as List<AssignmentDto>;
        Assert.That(assignments, Is.Empty);
    }

    [Test]
    public async Task GetAssignments_IncludesUserAssignments_ForMembersInManagerTeams()
    {
        _user.Teams.Returns([_team.Id]);

        Db.CourseAssignments.Add(new CourseAssignmentEntity
        {
            CourseId = _course.Id,
            UserId = "learner-1",
            TeamId = _team.Id,
            IsRequired = false,
            AssignedById = "coach-1",
        });
        await Db.SaveChangesAsync();

        var result = await _sut.GetAssignments();

        var ok = result.Result as OkObjectResult;
        var assignments = ok!.Value as List<AssignmentDto>;
        Assert.That(assignments, Has.Count.EqualTo(1));
        Assert.That(assignments![0].UserId, Is.EqualTo("learner-1"));
    }

    [Test]
    public async Task GetAssignments_IncludesCourseName()
    {
        _user.Teams.Returns([_team.Id]);

        Db.CourseAssignments.Add(new CourseAssignmentEntity
        {
            CourseId = _course.Id,
            TeamId = _team.Id,
            IsRequired = true,
            AssignedById = "coach-1",
        });
        await Db.SaveChangesAsync();

        var result = await _sut.GetAssignments();

        var ok = result.Result as OkObjectResult;
        var assignments = ok!.Value as List<AssignmentDto>;
        Assert.That(assignments![0].CourseName, Is.EqualTo("C# Basics"));
    }

    // POST

    [Test]
    public async Task AssignCourse_ToTeam_CreatesAssignment()
    {
        _user.UserId.Returns("coach-1");
        _user.Teams.Returns([_team.Id]);

        var request = new AssignCourseRequest(_course.Id, _team.Id, null, true);
        var result = await _sut.AssignCourse(request);

        Assert.That(result.Result, Is.TypeOf<CreatedAtActionResult>());
        var saved = Db.CourseAssignments.FirstOrDefault(a => a.CourseId == _course.Id);
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.TeamId, Is.EqualTo(_team.Id));
        Assert.That(saved.IsRequired, Is.True);
        Assert.That(saved.AssignedById, Is.EqualTo("coach-1"));
    }

    [Test]
    public async Task AssignCourse_ToUser_CreatesAssignment()
    {
        _user.UserId.Returns("coach-1");
        _user.Teams.Returns([_team.Id]);

        var request = new AssignCourseRequest(_course.Id, _team.Id, "learner-1", false);
        var result = await _sut.AssignCourse(request);

        Assert.That(result.Result, Is.TypeOf<CreatedAtActionResult>());
        var saved = Db.CourseAssignments.FirstOrDefault(a => a.UserId == "learner-1");
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.IsRequired, Is.False);
    }

    [Test]
    public async Task AssignCourse_WhenCourseNotFound_ReturnsNotFound()
    {
        _user.Teams.Returns([_team.Id]);

        var request = new AssignCourseRequest(999, _team.Id, null, true);
        var result = await _sut.AssignCourse(request);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task AssignCourse_WhenTeamNotInManagerTeams_ReturnsForbid()
    {
        _user.Teams.Returns([]); // manager has no teams

        var request = new AssignCourseRequest(_course.Id, _team.Id, null, true);
        var result = await _sut.AssignCourse(request);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    // DELETE

    [Test]
    public async Task RemoveAssignment_WhenExists_DeletesAndReturnsNoContent()
    {
        _user.Teams.Returns([_team.Id]);

        var assignment = new CourseAssignmentEntity
        {
            CourseId = _course.Id,
            TeamId = _team.Id,
            IsRequired = true,
            AssignedById = "coach-1",
        };
        Db.CourseAssignments.Add(assignment);
        await Db.SaveChangesAsync();

        var result = await _sut.RemoveAssignment(assignment.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(Db.CourseAssignments.Find(assignment.Id), Is.Null);
    }

    [Test]
    public async Task RemoveAssignment_WhenNotFound_ReturnsNotFound()
    {
        _user.Teams.Returns([_team.Id]);

        var result = await _sut.RemoveAssignment(999);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task RemoveAssignment_WhenTeamNotInManagerTeams_ReturnsForbid()
    {
        _user.Teams.Returns([]); // no access

        var assignment = new CourseAssignmentEntity
        {
            CourseId = _course.Id,
            TeamId = _team.Id,
            IsRequired = true,
            AssignedById = "coach-1",
        };
        Db.CourseAssignments.Add(assignment);
        await Db.SaveChangesAsync();

        var result = await _sut.RemoveAssignment(assignment.Id);

        Assert.That(result, Is.TypeOf<ForbidResult>());
    }
}
