using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class EnrollmentControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private EnrollmentController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _user.UserId.Returns("user1");
        _sut = new EnrollmentController(Db, _user);
    }

    // ─── POST /api/enrollment/{courseId} ─────────────────────────────────────

    [Test]
    public async Task Enroll_WhenCourseExists_ReturnsCreated()
    {
        var course = new CourseEntity { Name = "C# Basics" };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();

        var result = await _sut.Enroll(course.Id);

        Assert.That(result, Is.TypeOf<CreatedResult>());
        var enrollment = await Db.CourseEnrollments.FindAsync(((CreatedResult)result).Value is int id ? id : 0);
    }

    [Test]
    public async Task Enroll_WhenCourseExists_SavesEnrollmentToDatabase()
    {
        var course = new CourseEntity { Name = "C# Basics" };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();

        await _sut.Enroll(course.Id);

        var enrollment = Db.CourseEnrollments.FirstOrDefault(e => e.UserId == "user1" && e.CourseId == course.Id);
        Assert.That(enrollment, Is.Not.Null);
        Assert.That(enrollment!.EnrolledAt, Is.LessThanOrEqualTo(DateTime.UtcNow));
    }

    [Test]
    public async Task Enroll_WhenCourseNotFound_ReturnsNotFound()
    {
        var result = await _sut.Enroll(999);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task Enroll_WhenAlreadyEnrolled_ReturnsConflict()
    {
        var course = new CourseEntity { Name = "C# Basics" };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        Db.CourseEnrollments.Add(new CourseEnrollmentEntity { UserId = "user1", CourseId = course.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.Enroll(course.Id);

        Assert.That(result, Is.TypeOf<ConflictResult>());
    }

    // ─── DELETE /api/enrollment/{courseId} ───────────────────────────────────

    [Test]
    public async Task Unenroll_WhenEnrolled_ReturnsNoContent()
    {
        var course = new CourseEntity { Name = "C# Basics" };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        Db.CourseEnrollments.Add(new CourseEnrollmentEntity { UserId = "user1", CourseId = course.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.Unenroll(course.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
    }

    [Test]
    public async Task Unenroll_WhenEnrolled_RemovesEnrollmentFromDatabase()
    {
        var course = new CourseEntity { Name = "C# Basics" };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        Db.CourseEnrollments.Add(new CourseEnrollmentEntity { UserId = "user1", CourseId = course.Id });
        await Db.SaveChangesAsync();

        await _sut.Unenroll(course.Id);

        var enrollment = Db.CourseEnrollments.FirstOrDefault(e => e.UserId == "user1" && e.CourseId == course.Id);
        Assert.That(enrollment, Is.Null);
    }

    [Test]
    public async Task Unenroll_WhenNotEnrolled_ReturnsNotFound()
    {
        var result = await _sut.Unenroll(999);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    // ─── GET /api/enrollment ─────────────────────────────────────────────────

    [Test]
    public async Task GetMyEnrollments_ReturnsOnlyCurrentUserEnrollments()
    {
        var course1 = new CourseEntity { Name = "C# Basics" };
        var course2 = new CourseEntity { Name = "Advanced .NET" };
        Db.Courses.AddRange(course1, course2);
        await Db.SaveChangesAsync();
        Db.CourseEnrollments.Add(new CourseEnrollmentEntity { UserId = "user1", CourseId = course1.Id });
        Db.CourseEnrollments.Add(new CourseEnrollmentEntity { UserId = "other-user", CourseId = course2.Id });
        await Db.SaveChangesAsync();

        var result = await _sut.GetMyEnrollments();

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var enrollments = okResult!.Value as List<EnrollmentDto>;
        Assert.That(enrollments, Has.Count.EqualTo(1));
        Assert.That(enrollments![0].CourseName, Is.EqualTo("C# Basics"));
    }

    [Test]
    public async Task GetMyEnrollments_WhenNoEnrollments_ReturnsEmptyList()
    {
        var result = await _sut.GetMyEnrollments();

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var enrollments = okResult!.Value as List<EnrollmentDto>;
        Assert.That(enrollments, Is.Empty);
    }
}
