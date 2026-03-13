using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class SuggestionControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private SuggestionController _sut = null!;
    private const string UserId = "user-1";
    private const string ManagerId = "manager-1";

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _user.IsBackOffice.Returns(false);
        _user.IsManager.Returns(false);
        _user.UserId.Returns(UserId);
        _sut = new SuggestionController(Db, _user);
    }

    private async Task<CourseSuggestionEntity> SeedSuggestion(
        string? userId = null,
        string title = "React Course",
        SuggestionStatus status = SuggestionStatus.Pending)
    {
        var suggestion = new CourseSuggestionEntity
        {
            UserId = userId ?? UserId,
            Title = title,
            Status = status,
        };
        Db.CourseSuggestions.Add(suggestion);
        await Db.SaveChangesAsync();
        return suggestion;
    }

    // ─── GetSuggestions ───────────────────────────────────────────────────────

    [Test]
    public async Task GetSuggestions_Learner_ReturnsOnlyOwn()
    {
        await SeedSuggestion(UserId);
        await SeedSuggestion("other-user");

        var result = await _sut.GetSuggestions();

        Assert.That(result.Value!, Has.Count.EqualTo(1));
        Assert.That(result.Value![0].UserId, Is.EqualTo(UserId));
    }

    [Test]
    public async Task GetSuggestions_Manager_ReturnsAll()
    {
        _user.IsManager.Returns(true);
        await SeedSuggestion(UserId);
        await SeedSuggestion("other-user");

        var result = await _sut.GetSuggestions();

        Assert.That(result.Value!, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetSuggestions_BackOffice_ReturnsAll()
    {
        _user.IsBackOffice.Returns(true);
        await SeedSuggestion(UserId);
        await SeedSuggestion("other-user");

        var result = await _sut.GetSuggestions();

        Assert.That(result.Value!, Has.Count.EqualTo(2));
    }

    // ─── Submit ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Submit_CreatesSuggestion()
    {
        var result = await _sut.Submit(new SuggestionRequest("Docker Course", "Learn containers", "Team needs DevOps skills"));

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var suggestion = created!.Value as CourseSuggestionEntity;
        Assert.That(suggestion!.UserId, Is.EqualTo(UserId));
        Assert.That(suggestion.Title, Is.EqualTo("Docker Course"));
        Assert.That(suggestion.Status, Is.EqualTo(SuggestionStatus.Pending));
        Assert.That(await Db.CourseSuggestions.FindAsync(suggestion.Id), Is.Not.Null);
    }

    [Test]
    public async Task Submit_SetsDefaultStatusToPending()
    {
        var result = await _sut.Submit(new SuggestionRequest("Kubernetes", null, null));

        var created = result.Result as CreatedAtActionResult;
        var suggestion = created!.Value as CourseSuggestionEntity;
        Assert.That(suggestion!.Status, Is.EqualTo(SuggestionStatus.Pending));
    }

    // ─── Review ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Review_Manager_ApprovesSuggestion()
    {
        _user.IsManager.Returns(true);
        _user.UserId.Returns(ManagerId);
        var suggestion = await SeedSuggestion();

        var result = await _sut.Review(suggestion.Id, new ReviewSuggestionRequest(SuggestionStatus.Approved, "Great idea!"));

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var updated = await Db.CourseSuggestions.FindAsync(suggestion.Id);
        Assert.That(updated!.Status, Is.EqualTo(SuggestionStatus.Approved));
        Assert.That(updated.ReviewedBy, Is.EqualTo(ManagerId));
        Assert.That(updated.ReviewedAt, Is.Not.Null);
        Assert.That(updated.ReviewNote, Is.EqualTo("Great idea!"));
    }

    [Test]
    public async Task Review_Manager_RejectsSuggestion()
    {
        _user.IsManager.Returns(true);
        var suggestion = await SeedSuggestion();

        var result = await _sut.Review(suggestion.Id, new ReviewSuggestionRequest(SuggestionStatus.Rejected, "Out of scope"));

        Assert.That(result, Is.TypeOf<NoContentResult>());
        var updated = await Db.CourseSuggestions.FindAsync(suggestion.Id);
        Assert.That(updated!.Status, Is.EqualTo(SuggestionStatus.Rejected));
    }

    [Test]
    public async Task Review_Learner_ReturnsForbid()
    {
        var suggestion = await SeedSuggestion();

        var result = await _sut.Review(suggestion.Id, new ReviewSuggestionRequest(SuggestionStatus.Approved, null));

        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task Review_NotFound_ReturnsNotFound()
    {
        _user.IsManager.Returns(true);

        var result = await _sut.Review(999, new ReviewSuggestionRequest(SuggestionStatus.Approved, null));

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    // ─── Delete ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Delete_OwnSuggestion_Removes()
    {
        var suggestion = await SeedSuggestion();

        var result = await _sut.Delete(suggestion.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(await Db.CourseSuggestions.FindAsync(suggestion.Id), Is.Null);
    }

    [Test]
    public async Task Delete_OtherUser_ReturnsForbid()
    {
        var suggestion = await SeedSuggestion("other-user");

        var result = await _sut.Delete(suggestion.Id);

        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task Delete_BackOffice_CanDeleteAny()
    {
        _user.IsBackOffice.Returns(true);
        var suggestion = await SeedSuggestion("other-user");

        var result = await _sut.Delete(suggestion.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_NotFound_ReturnsNotFound()
    {
        var result = await _sut.Delete(999);
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }
}
