using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class FeedbackControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private FeedbackController _sut = null!;
    private const string UserId = "user-1";

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _user.IsBackOffice.Returns(false);
        _user.UserId.Returns(UserId);
        _sut = new FeedbackController(Db, _user);
    }

    private async Task<CourseEntity> SeedCourse(string name = "Test Course")
    {
        var course = new CourseEntity { Name = name, Status = CourseStatus.Published };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        return course;
    }

    private async Task<FeedbackEntity> SeedFeedback(int courseId, string? userId = null, int rating = 4)
    {
        var feedback = new FeedbackEntity
        {
            UserId = userId ?? UserId,
            CourseId = courseId,
            Rating = rating,
            Comment = "Good course",
        };
        Db.Feedback.Add(feedback);
        await Db.SaveChangesAsync();
        return feedback;
    }

    // ─── GetFeedback ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetFeedback_BackOffice_ReturnsAllFeedback()
    {
        _user.IsBackOffice.Returns(true);
        var course = await SeedCourse();
        await SeedFeedback(course.Id, "user-1");
        await SeedFeedback(course.Id, "user-2");

        var result = await _sut.GetFeedback(null);

        Assert.That(result.Value!, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetFeedback_FilterByCourse_ReturnsOnlyThatCourse()
    {
        _user.IsBackOffice.Returns(true);
        var course1 = await SeedCourse("C#");
        var course2 = await SeedCourse("Java");
        await SeedFeedback(course1.Id);
        await SeedFeedback(course2.Id);

        var result = await _sut.GetFeedback(course1.Id);

        Assert.That(result.Value!, Has.Count.EqualTo(1));
        Assert.That(result.Value![0].CourseId, Is.EqualTo(course1.Id));
    }

    [Test]
    public async Task GetFeedback_NonBackOffice_ReturnsOwnFeedback()
    {
        var course = await SeedCourse();
        await SeedFeedback(course.Id, UserId);
        await SeedFeedback(course.Id, "other-user");

        var result = await _sut.GetFeedback(null);

        Assert.That(result.Value!, Has.Count.EqualTo(1));
        Assert.That(result.Value![0].UserId, Is.EqualTo(UserId));
    }

    [Test]
    public async Task GetFeedback_IncludesCourseData()
    {
        var course = await SeedCourse("C#");
        await SeedFeedback(course.Id);

        var result = await _sut.GetFeedback(null);

        Assert.That(result.Value![0].Course.Name, Is.EqualTo("C#"));
    }

    // ─── SubmitFeedback ───────────────────────────────────────────────────────

    [Test]
    public async Task SubmitFeedback_CreatesFeedback()
    {
        var course = await SeedCourse();

        var result = await _sut.SubmitFeedback(course.Id, new FeedbackRequest(5, "Excellent!"));

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var feedback = created!.Value as FeedbackEntity;
        Assert.That(feedback!.Rating, Is.EqualTo(5));
        Assert.That(feedback.Comment, Is.EqualTo("Excellent!"));
        Assert.That(feedback.UserId, Is.EqualTo(UserId));
        Assert.That(await Db.Feedback.FindAsync(feedback.Id), Is.Not.Null);
    }

    [Test]
    public async Task SubmitFeedback_CourseNotFound_ReturnsNotFound()
    {
        var result = await _sut.SubmitFeedback(999, new FeedbackRequest(5, null));
        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task SubmitFeedback_AlreadySubmitted_ReturnsBadRequest()
    {
        var course = await SeedCourse();
        await SeedFeedback(course.Id);

        var result = await _sut.SubmitFeedback(course.Id, new FeedbackRequest(3, null));

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SubmitFeedback_InvalidRating_ReturnsBadRequest()
    {
        var course = await SeedCourse();

        var result = await _sut.SubmitFeedback(course.Id, new FeedbackRequest(6, null));

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    // ─── DeleteFeedback ───────────────────────────────────────────────────────

    [Test]
    public async Task DeleteFeedback_OwnFeedback_Removes()
    {
        var course = await SeedCourse();
        var feedback = await SeedFeedback(course.Id);

        var result = await _sut.DeleteFeedback(feedback.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(await Db.Feedback.FindAsync(feedback.Id), Is.Null);
    }

    [Test]
    public async Task DeleteFeedback_OtherUser_ReturnsForbid()
    {
        var course = await SeedCourse();
        var feedback = await SeedFeedback(course.Id, "other-user");

        var result = await _sut.DeleteFeedback(feedback.Id);

        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task DeleteFeedback_BackOffice_CanDeleteAny()
    {
        _user.IsBackOffice.Returns(true);
        var course = await SeedCourse();
        var feedback = await SeedFeedback(course.Id, "other-user");

        var result = await _sut.DeleteFeedback(feedback.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteFeedback_NotFound_ReturnsNotFound()
    {
        var result = await _sut.DeleteFeedback(999);
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }
}
