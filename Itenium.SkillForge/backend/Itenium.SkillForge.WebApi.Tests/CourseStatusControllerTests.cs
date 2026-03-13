using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class CourseStatusControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private CourseController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _sut = new CourseController(Db, _user);
    }

    // GET filtering

    [Test]
    public async Task GetCourses_AsLearner_OnlyReturnsPublished()
    {
        _user.IsManager.Returns(false);
        _user.IsBackOffice.Returns(false);

        Db.Courses.AddRange(
            new CourseEntity { Name = "Draft", Status = CourseStatus.Draft },
            new CourseEntity { Name = "Published", Status = CourseStatus.Published },
            new CourseEntity { Name = "Archived", Status = CourseStatus.Archived });
        await Db.SaveChangesAsync();

        var result = await _sut.GetCourses();

        var ok = result.Result as OkObjectResult;
        var courses = ok!.Value as List<CourseEntity>;
        Assert.That(courses, Has.Count.EqualTo(1));
        Assert.That(courses![0].Name, Is.EqualTo("Published"));
    }

    [Test]
    public async Task GetCourses_AsManager_ReturnsAllStatuses()
    {
        _user.IsManager.Returns(true);

        Db.Courses.AddRange(
            new CourseEntity { Name = "Draft", Status = CourseStatus.Draft },
            new CourseEntity { Name = "Published", Status = CourseStatus.Published },
            new CourseEntity { Name = "Archived", Status = CourseStatus.Archived });
        await Db.SaveChangesAsync();

        var result = await _sut.GetCourses();

        var ok = result.Result as OkObjectResult;
        var courses = ok!.Value as List<CourseEntity>;
        Assert.That(courses, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetCourses_AsBackOffice_ReturnsAllStatuses()
    {
        _user.IsBackOffice.Returns(true);

        Db.Courses.AddRange(
            new CourseEntity { Name = "Draft", Status = CourseStatus.Draft },
            new CourseEntity { Name = "Published", Status = CourseStatus.Published });
        await Db.SaveChangesAsync();

        var result = await _sut.GetCourses();

        var ok = result.Result as OkObjectResult;
        var courses = ok!.Value as List<CourseEntity>;
        Assert.That(courses, Has.Count.EqualTo(2));
    }

    // Status filter param

    [Test]
    public async Task GetCourses_WithStatusFilter_ReturnsOnlyMatchingStatus()
    {
        _user.IsManager.Returns(true);

        Db.Courses.AddRange(
            new CourseEntity { Name = "Draft", Status = CourseStatus.Draft },
            new CourseEntity { Name = "Published", Status = CourseStatus.Published });
        await Db.SaveChangesAsync();

        var result = await _sut.GetCourses(status: CourseStatus.Draft);

        var ok = result.Result as OkObjectResult;
        var courses = ok!.Value as List<CourseEntity>;
        Assert.That(courses, Has.Count.EqualTo(1));
        Assert.That(courses![0].Name, Is.EqualTo("Draft"));
    }

    // PUT /status

    [Test]
    public async Task SetStatus_WhenExists_UpdatesStatus()
    {
        _user.IsManager.Returns(true);

        var course = new CourseEntity { Name = "C# Basics", Status = CourseStatus.Draft };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();

        var result = await _sut.SetStatus(course.Id, new SetCourseStatusRequest(CourseStatus.Published));

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var updated = ok!.Value as CourseEntity;
        Assert.That(updated!.Status, Is.EqualTo(CourseStatus.Published));

        var saved = await Db.Courses.FindAsync(course.Id);
        Assert.That(saved!.Status, Is.EqualTo(CourseStatus.Published));
    }

    [Test]
    public async Task SetStatus_WhenNotFound_ReturnsNotFound()
    {
        var result = await _sut.SetStatus(999, new SetCourseStatusRequest(CourseStatus.Published));
        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    // Create defaults to Draft

    [Test]
    public async Task CreateCourse_DefaultsStatusToDraft()
    {
        var result = await _sut.CreateCourse(new CreateCourseRequest("New Course", null, null, null));

        var created = result.Result as CreatedAtActionResult;
        var course = created!.Value as CourseEntity;
        Assert.That(course!.Status, Is.EqualTo(CourseStatus.Draft));
    }
}
