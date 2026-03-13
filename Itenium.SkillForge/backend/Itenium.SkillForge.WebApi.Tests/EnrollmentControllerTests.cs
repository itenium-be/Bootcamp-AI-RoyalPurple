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
    private const string UserId = "user-1";

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _user.IsBackOffice.Returns(false);
        _user.UserId.Returns(UserId);
        _sut = new EnrollmentController(Db, _user);
    }

    private async Task<CourseEntity> SeedCourse(string name = "Test Course")
    {
        var course = new CourseEntity { Name = name, Status = CourseStatus.Published };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        return course;
    }

    private async Task<EnrollmentEntity> SeedEnrollment(int courseId, string? userId = null, EnrollmentStatus status = EnrollmentStatus.Enrolled)
    {
        var enrollment = new EnrollmentEntity
        {
            UserId = userId ?? UserId,
            CourseId = courseId,
            Status = status,
        };
        Db.Enrollments.Add(enrollment);
        await Db.SaveChangesAsync();
        return enrollment;
    }

    // ─── GetEnrollments ───────────────────────────────────────────────────────

    [Test]
    public async Task GetEnrollments_ReturnsOnlyCurrentUserEnrollments()
    {
        var course = await SeedCourse();
        await SeedEnrollment(course.Id, UserId);
        await SeedEnrollment(course.Id, "other-user");

        var result = await _sut.GetEnrollments();

        var enrollments = result.Value!;
        Assert.That(enrollments, Has.Count.EqualTo(1));
        Assert.That(enrollments[0].UserId, Is.EqualTo(UserId));
    }

    [Test]
    public async Task GetEnrollments_BackOffice_ReturnsAllEnrollments()
    {
        _user.IsBackOffice.Returns(true);
        var course = await SeedCourse();
        await SeedEnrollment(course.Id, UserId);
        await SeedEnrollment(course.Id, "other-user");

        var result = await _sut.GetEnrollments();

        Assert.That(result.Value!, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetEnrollments_IncludesCourseData()
    {
        var course = await SeedCourse("C#");
        await SeedEnrollment(course.Id);

        var result = await _sut.GetEnrollments();

        Assert.That(result.Value![0].Course.Name, Is.EqualTo("C#"));
    }

    // ─── Enroll ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Enroll_CreatesEnrollment()
    {
        var course = await SeedCourse();

        var result = await _sut.Enroll(course.Id);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var enrollment = created!.Value as EnrollmentEntity;
        Assert.That(enrollment!.UserId, Is.EqualTo(UserId));
        Assert.That(enrollment.CourseId, Is.EqualTo(course.Id));
        Assert.That(enrollment.Status, Is.EqualTo(EnrollmentStatus.Enrolled));
        Assert.That(await Db.Enrollments.FindAsync(enrollment.Id), Is.Not.Null);
    }

    [Test]
    public async Task Enroll_CourseNotFound_ReturnsNotFound()
    {
        var result = await _sut.Enroll(999);
        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task Enroll_AlreadyEnrolled_ReturnsBadRequest()
    {
        var course = await SeedCourse();
        await SeedEnrollment(course.Id);

        var result = await _sut.Enroll(course.Id);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    // ─── UpdateStatus ─────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateStatus_UpdatesEnrollmentStatus()
    {
        var course = await SeedCourse();
        var enrollment = await SeedEnrollment(course.Id);

        var result = await _sut.UpdateStatus(enrollment.Id, new UpdateEnrollmentStatusRequest(EnrollmentStatus.InProgress));

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var updated = await Db.Enrollments.FindAsync(enrollment.Id);
        Assert.That(updated!.Status, Is.EqualTo(EnrollmentStatus.InProgress));
    }

    [Test]
    public async Task UpdateStatus_Completed_SetsCompletedAt()
    {
        var course = await SeedCourse();
        var enrollment = await SeedEnrollment(course.Id, status: EnrollmentStatus.InProgress);

        await _sut.UpdateStatus(enrollment.Id, new UpdateEnrollmentStatusRequest(EnrollmentStatus.Completed));

        var updated = await Db.Enrollments.FindAsync(enrollment.Id);
        Assert.That(updated!.CompletedAt, Is.Not.Null);
    }

    [Test]
    public async Task UpdateStatus_OtherUserEnrollment_ReturnsForbid()
    {
        var course = await SeedCourse();
        var enrollment = await SeedEnrollment(course.Id, "other-user");

        var result = await _sut.UpdateStatus(enrollment.Id, new UpdateEnrollmentStatusRequest(EnrollmentStatus.InProgress));

        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task UpdateStatus_NotFound_ReturnsNotFound()
    {
        var result = await _sut.UpdateStatus(999, new UpdateEnrollmentStatusRequest(EnrollmentStatus.InProgress));
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    // ─── Unenroll ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Unenroll_RemovesEnrollment()
    {
        var course = await SeedCourse();
        var enrollment = await SeedEnrollment(course.Id);

        var result = await _sut.Unenroll(enrollment.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(await Db.Enrollments.FindAsync(enrollment.Id), Is.Null);
    }

    [Test]
    public async Task Unenroll_OtherUserEnrollment_ReturnsForbid()
    {
        var course = await SeedCourse();
        var enrollment = await SeedEnrollment(course.Id, "other-user");

        var result = await _sut.Unenroll(enrollment.Id);

        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task Unenroll_BackOffice_CanRemoveAnyEnrollment()
    {
        _user.IsBackOffice.Returns(true);
        var course = await SeedCourse();
        var enrollment = await SeedEnrollment(course.Id, "other-user");

        var result = await _sut.Unenroll(enrollment.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
    }

    [Test]
    public async Task Unenroll_NotFound_ReturnsNotFound()
    {
        var result = await _sut.Unenroll(999);
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }
}
