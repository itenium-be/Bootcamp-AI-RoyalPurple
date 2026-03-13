using Itenium.SkillForge.Entities;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class CourseResourceControllerTests : DatabaseTestBase
{
    private CourseResourceController _sut = null!;
    private CourseEntity _course = null!;

    [SetUp]
    public async Task Setup()
    {
        _sut = new CourseResourceController(Db);

        _course = new CourseEntity { Name = "C# Fundamentals", Category = ".NET" };
        Db.Courses.Add(_course);
        await Db.SaveChangesAsync();
    }

    // GET /api/course/{id}/resource

    [Test]
    public async Task GetResources_ReturnsResourcesOrderedByOrder()
    {
        Db.CourseResources.AddRange(
            new CourseResourceEntity { CourseId = _course.Id, Title = "Chapter 2", Type = CourseResourceType.Article, Order = 2 },
            new CourseResourceEntity { CourseId = _course.Id, Title = "Chapter 1", Type = CourseResourceType.Video, Order = 1 });
        await Db.SaveChangesAsync();

        var result = await _sut.GetResources(_course.Id);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var resources = ok!.Value as IList<CourseResourceEntity>;
        Assert.That(resources, Has.Count.EqualTo(2));
        Assert.That(resources![0].Title, Is.EqualTo("Chapter 1"));
        Assert.That(resources[1].Title, Is.EqualTo("Chapter 2"));
    }

    [Test]
    public async Task GetResources_WhenCourseNotFound_ReturnsNotFound()
    {
        var result = await _sut.GetResources(999);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task GetResources_OnlyReturnsResourcesForThatCourse()
    {
        var otherCourse = new CourseEntity { Name = "Java Basics" };
        Db.Courses.Add(otherCourse);
        await Db.SaveChangesAsync();

        Db.CourseResources.AddRange(
            new CourseResourceEntity { CourseId = _course.Id, Title = "Mine", Type = CourseResourceType.Article, Order = 1 },
            new CourseResourceEntity { CourseId = otherCourse.Id, Title = "Not mine", Type = CourseResourceType.Video, Order = 1 });
        await Db.SaveChangesAsync();

        var result = await _sut.GetResources(_course.Id);

        var ok = result.Result as OkObjectResult;
        var resources = ok!.Value as IList<CourseResourceEntity>;
        Assert.That(resources, Has.Count.EqualTo(1));
        Assert.That(resources![0].Title, Is.EqualTo("Mine"));
    }

    // GET /api/course/{id}/resource/{resourceId}

    [Test]
    public async Task GetResource_ReturnsResource()
    {
        var resource = new CourseResourceEntity { CourseId = _course.Id, Title = "Intro Video", Type = CourseResourceType.Video, Order = 1 };
        Db.CourseResources.Add(resource);
        await Db.SaveChangesAsync();

        var result = await _sut.GetResource(_course.Id, resource.Id);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That((ok!.Value as CourseResourceEntity)!.Title, Is.EqualTo("Intro Video"));
    }

    [Test]
    public async Task GetResource_WhenCourseNotFound_ReturnsNotFound()
    {
        var result = await _sut.GetResource(999, 1);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task GetResource_WhenResourceNotFound_ReturnsNotFound()
    {
        var result = await _sut.GetResource(_course.Id, 999);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task GetResource_WhenResourceBelongsToDifferentCourse_ReturnsNotFound()
    {
        var otherCourse = new CourseEntity { Name = "Java Basics" };
        Db.Courses.Add(otherCourse);
        await Db.SaveChangesAsync();
        var resource = new CourseResourceEntity { CourseId = otherCourse.Id, Title = "Other", Type = CourseResourceType.Article, Order = 1 };
        Db.CourseResources.Add(resource);
        await Db.SaveChangesAsync();

        var result = await _sut.GetResource(_course.Id, resource.Id);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    // POST /api/course/{id}/resource

    [Test]
    public async Task CreateResource_CreatesAndReturns201()
    {
        var request = new CourseResourceRequest("Intro Video", "https://example.com", CourseResourceType.Video, "An intro", 10, 1, null, null);

        var result = await _sut.CreateResource(_course.Id, request);

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var resource = created!.Value as CourseResourceEntity;
        Assert.That(resource!.Title, Is.EqualTo("Intro Video"));
        Assert.That(resource.CourseId, Is.EqualTo(_course.Id));
    }

    [Test]
    public async Task CreateResource_WhenCourseNotFound_ReturnsNotFound()
    {
        var request = new CourseResourceRequest("Intro Video", null, CourseResourceType.Video, null, null, 1, null, null);

        var result = await _sut.CreateResource(999, request);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task CreateResource_PersistsToDatabase()
    {
        var request = new CourseResourceRequest("Article", null, CourseResourceType.Article, null, null, 1, null, null);

        await _sut.CreateResource(_course.Id, request);

        Assert.That(await Db.CourseResources.CountAsync(), Is.EqualTo(1));
    }

    // PUT /api/course/{id}/resource/{resourceId}

    [Test]
    public async Task UpdateResource_UpdatesAndReturnsResource()
    {
        var resource = new CourseResourceEntity { CourseId = _course.Id, Title = "Old Title", Type = CourseResourceType.Article, Order = 1 };
        Db.CourseResources.Add(resource);
        await Db.SaveChangesAsync();

        var request = new CourseResourceRequest("New Title", "https://example.com", CourseResourceType.Video, "Updated", 15, 2, null, null);

        var result = await _sut.UpdateResource(_course.Id, resource.Id, request);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var updated = ok!.Value as CourseResourceEntity;
        Assert.That(updated!.Title, Is.EqualTo("New Title"));
        Assert.That(updated.Order, Is.EqualTo(2));
    }

    [Test]
    public async Task UpdateResource_WhenNotFound_ReturnsNotFound()
    {
        var request = new CourseResourceRequest("Title", null, CourseResourceType.Article, null, null, 1, null, null);

        var result = await _sut.UpdateResource(_course.Id, 999, request);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    // DELETE /api/course/{id}/resource/{resourceId}

    [Test]
    public async Task DeleteResource_RemovesResource()
    {
        var resource = new CourseResourceEntity { CourseId = _course.Id, Title = "To Delete", Type = CourseResourceType.Article, Order = 1 };
        Db.CourseResources.Add(resource);
        await Db.SaveChangesAsync();

        var result = await _sut.DeleteResource(_course.Id, resource.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(await Db.CourseResources.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task DeleteResource_WhenNotFound_ReturnsNotFound()
    {
        var result = await _sut.DeleteResource(_course.Id, 999);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    // Seed data

    [Test]
    public async Task SeedData_CoursesHaveResources()
    {
        await Itenium.SkillForge.Data.SeedData.SeedDevelopmentData_ForTest(Db);

        var courses = await Db.Courses.Include(c => c.Resources).ToListAsync();
        Assert.That(courses.Any(c => c.Resources.Count > 0), Is.True);
    }
}
