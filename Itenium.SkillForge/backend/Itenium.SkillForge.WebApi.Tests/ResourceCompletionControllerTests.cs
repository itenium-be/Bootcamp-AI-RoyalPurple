using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class ResourceCompletionControllerTests : DatabaseTestBase
{
    private CourseResourceController _sut = null!;
    private ISkillForgeUser _currentUser = null!;
    private CourseEntity _course = null!;
    private CourseResourceEntity _resource = null!;
    private const string UserId = "user-123";

    [SetUp]
    public async Task Setup()
    {
        _currentUser = Substitute.For<ISkillForgeUser>();
        _currentUser.UserId.Returns(UserId);

        _sut = new CourseResourceController(Db, _currentUser);

        _course = new CourseEntity { Name = "C# Fundamentals" };
        Db.Courses.Add(_course);
        await Db.SaveChangesAsync();

        _resource = new CourseResourceEntity { CourseId = _course.Id, Title = "Intro Video", Type = CourseResourceType.Video, Order = 1 };
        Db.CourseResources.Add(_resource);
        await Db.SaveChangesAsync();
    }

    // GET /api/course/{id}/resource — includes completion status

    [Test]
    public async Task GetResources_IncludesIsCompleted_False_WhenNotCompleted()
    {
        var result = await _sut.GetResources(_course.Id);

        var ok = result.Result as OkObjectResult;
        var resources = ok!.Value as IList<CourseResourceDto>;
        Assert.That(resources![0].IsCompleted, Is.False);
    }

    [Test]
    public async Task GetResources_IncludesIsCompleted_True_WhenCompletedByCurrentUser()
    {
        Db.ResourceCompletions.Add(new ResourceCompletionEntity { UserId = UserId, ResourceId = _resource.Id, CompletedAt = DateTime.UtcNow });
        await Db.SaveChangesAsync();

        var result = await _sut.GetResources(_course.Id);

        var ok = result.Result as OkObjectResult;
        var resources = ok!.Value as IList<CourseResourceDto>;
        Assert.That(resources![0].IsCompleted, Is.True);
    }

    [Test]
    public async Task GetResources_IsCompleted_False_WhenCompletedByDifferentUser()
    {
        Db.ResourceCompletions.Add(new ResourceCompletionEntity { UserId = "other-user", ResourceId = _resource.Id, CompletedAt = DateTime.UtcNow });
        await Db.SaveChangesAsync();

        var result = await _sut.GetResources(_course.Id);

        var ok = result.Result as OkObjectResult;
        var resources = ok!.Value as IList<CourseResourceDto>;
        Assert.That(resources![0].IsCompleted, Is.False);
    }

    [Test]
    public async Task GetResources_IncludesSkillName_WhenLinked()
    {
        var skill = new SkillEntity { Name = "C#", LevelCount = 7, CategoryId = 1 };
        Db.SkillCategories.Add(new SkillCategoryEntity { Name = "Backend" });
        await Db.SaveChangesAsync();
        skill.CategoryId = Db.SkillCategories.First().Id;
        Db.Skills.Add(skill);
        await Db.SaveChangesAsync();

        _resource.SkillId = skill.Id;
        _resource.ToLevel = 3;
        await Db.SaveChangesAsync();

        var result = await _sut.GetResources(_course.Id);

        var ok = result.Result as OkObjectResult;
        var resources = ok!.Value as IList<CourseResourceDto>;
        Assert.That(resources![0].SkillName, Is.EqualTo("C#"));
        Assert.That(resources![0].ToLevel, Is.EqualTo(3));
    }

    // POST /api/course/{id}/resource/{resourceId}/complete

    [Test]
    public async Task MarkComplete_RecordsCompletion()
    {
        var result = await _sut.MarkComplete(_course.Id, _resource.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(await Db.ResourceCompletions.CountAsync(), Is.EqualTo(1));
        var completion = await Db.ResourceCompletions.FirstAsync();
        Assert.That(completion.UserId, Is.EqualTo(UserId));
        Assert.That(completion.ResourceId, Is.EqualTo(_resource.Id));
    }

    [Test]
    public async Task MarkComplete_WhenAlreadyCompleted_Returns409()
    {
        Db.ResourceCompletions.Add(new ResourceCompletionEntity { UserId = UserId, ResourceId = _resource.Id, CompletedAt = DateTime.UtcNow });
        await Db.SaveChangesAsync();

        var result = await _sut.MarkComplete(_course.Id, _resource.Id);

        Assert.That(result, Is.TypeOf<ConflictResult>());
        Assert.That(await Db.ResourceCompletions.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task MarkComplete_WhenResourceNotFound_ReturnsNotFound()
    {
        var result = await _sut.MarkComplete(_course.Id, 999);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    // DELETE /api/course/{id}/resource/{resourceId}/complete

    [Test]
    public async Task UnmarkComplete_RemovesCompletion()
    {
        Db.ResourceCompletions.Add(new ResourceCompletionEntity { UserId = UserId, ResourceId = _resource.Id, CompletedAt = DateTime.UtcNow });
        await Db.SaveChangesAsync();

        var result = await _sut.UnmarkComplete(_course.Id, _resource.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(await Db.ResourceCompletions.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task UnmarkComplete_WhenNotCompleted_ReturnsNotFound()
    {
        var result = await _sut.UnmarkComplete(_course.Id, _resource.Id);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }
}
