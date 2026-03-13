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

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _sut = new FeedbackController(Db, _user);
    }

    private FeedbackEntity AddFeedback(string authorId, string recipientId, string content = "Test feedback", int? courseId = null)
    {
        var feedback = new FeedbackEntity
        {
            AuthorId = authorId,
            RecipientId = recipientId,
            Content = content,
            CourseId = courseId,
        };
        Db.Feedbacks.Add(feedback);
        return feedback;
    }

    // ─── GET /api/feedback ────────────────────────────────────────────────────

    [Test]
    public async Task GetFeedback_WhenBackOffice_ReturnsAllFeedback()
    {
        AddFeedback("manager1", "admin1");
        AddFeedback("learner1", "manager1");
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(true);
        _user.UserId.Returns("admin1");

        var result = await _sut.GetFeedback();

        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetFeedback_WhenManager_ReturnsOwnSentAndReceived()
    {
        AddFeedback("manager1", "admin1");   // sent by manager1
        AddFeedback("learner1", "manager1"); // received by manager1
        AddFeedback("learner2", "manager2"); // unrelated
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.UserId.Returns("manager1");

        var result = await _sut.GetFeedback();

        Assert.That(result.Value, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetFeedback_WhenLearner_ReturnsOnlyOwnFeedback()
    {
        AddFeedback("learner1", "manager1");
        AddFeedback("learner2", "manager1");
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.UserId.Returns("learner1");

        var result = await _sut.GetFeedback();

        Assert.That(result.Value, Has.Count.EqualTo(1));
    }

    // ─── GET /api/feedback/{id} ───────────────────────────────────────────────

    [Test]
    public async Task GetFeedbackById_WhenAuthor_ReturnsFeedbackWithComments()
    {
        var feedback = AddFeedback("manager1", "admin1", "Please review");
        await Db.SaveChangesAsync();
        Db.FeedbackComments.Add(new FeedbackCommentEntity
        {
            FeedbackId = feedback.Id,
            AuthorId = "admin1",
            Content = "On it",
        });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.UserId.Returns("manager1");

        var result = await _sut.GetFeedbackById(feedback.Id);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var detail = ok!.Value as FeedbackDetailDto;
        Assert.That(detail!.Content, Is.EqualTo("Please review"));
        Assert.That(detail.Comments, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetFeedbackById_WhenRecipient_ReturnsFeedback()
    {
        var feedback = AddFeedback("manager1", "admin1");
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.UserId.Returns("admin1");

        var result = await _sut.GetFeedbackById(feedback.Id);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
    }

    [Test]
    public async Task GetFeedbackById_WhenBackOffice_ReturnsFeedback()
    {
        var feedback = AddFeedback("manager1", "admin1");
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(true);
        _user.UserId.Returns("other_admin");

        var result = await _sut.GetFeedbackById(feedback.Id);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
    }

    [Test]
    public async Task GetFeedbackById_WhenUnrelated_ReturnsForbidden()
    {
        var feedback = AddFeedback("manager1", "admin1");
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.UserId.Returns("unrelated");

        var result = await _sut.GetFeedbackById(feedback.Id);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task GetFeedbackById_WhenNotFound_ReturnsNotFound()
    {
        _user.IsBackOffice.Returns(false);
        _user.UserId.Returns("manager1");

        var result = await _sut.GetFeedbackById(999);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    // ─── POST /api/feedback ───────────────────────────────────────────────────

    [Test]
    public async Task CreateFeedback_StoresFeedbackWithAuthorId()
    {
        _user.UserId.Returns("manager1");
        var request = new CreateFeedbackRequest("admin1", "Great team culture!", null);

        var result = await _sut.CreateFeedback(request);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var dto = created!.Value as FeedbackDto;
        Assert.That(dto!.AuthorId, Is.EqualTo("manager1"));
        Assert.That(dto.RecipientId, Is.EqualTo("admin1"));
        Assert.That(dto.Content, Is.EqualTo("Great team culture!"));
        Assert.That(dto.AuthorName, Is.Null); // no real user records in test DB

        var saved = await Db.Feedbacks.FindAsync(dto.Id);
        Assert.That(saved, Is.Not.Null);
    }

    [Test]
    public async Task CreateFeedback_WithCourse_IncludesCourseId()
    {
        var course = new CourseEntity { Name = "C# Basics" };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        _user.UserId.Returns("learner1");
        var request = new CreateFeedbackRequest("manager1", "Loved this course", course.Id);

        var result = await _sut.CreateFeedback(request);

        var created = result.Result as CreatedAtActionResult;
        var dto = created!.Value as FeedbackDto;
        Assert.That(dto!.CourseId, Is.EqualTo(course.Id));
    }

    // ─── POST /api/feedback/{id}/comments ─────────────────────────────────────

    [Test]
    public async Task AddComment_WhenAuthor_AddsComment()
    {
        var feedback = AddFeedback("manager1", "admin1");
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.UserId.Returns("manager1");
        var request = new CreateCommentRequest("Any update?");

        var result = await _sut.AddComment(feedback.Id, request);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var comment = created!.Value as FeedbackCommentDto;
        Assert.That(comment!.AuthorId, Is.EqualTo("manager1"));
        Assert.That(comment.Content, Is.EqualTo("Any update?"));
    }

    [Test]
    public async Task AddComment_WhenRecipient_AddsComment()
    {
        var feedback = AddFeedback("manager1", "admin1");
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.UserId.Returns("admin1");

        var result = await _sut.AddComment(feedback.Id, new CreateCommentRequest("Noted, thanks"));

        Assert.That(result.Result, Is.TypeOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task AddComment_WhenUnrelated_ReturnsForbidden()
    {
        var feedback = AddFeedback("manager1", "admin1");
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.UserId.Returns("unrelated");

        var result = await _sut.AddComment(feedback.Id, new CreateCommentRequest("Sneaky comment"));

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task AddComment_WhenFeedbackNotFound_ReturnsNotFound()
    {
        _user.IsBackOffice.Returns(false);
        _user.UserId.Returns("manager1");

        var result = await _sut.AddComment(999, new CreateCommentRequest("Comment"));

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }
}
