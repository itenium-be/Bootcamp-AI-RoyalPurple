using System.Globalization;
using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class DashboardControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private DashboardController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _sut = new DashboardController(Db, _user);
    }

    private async Task<ForgeUser> SeedUser(string username, bool locked = false, int[]? teams = null)
    {
        var user = new ForgeUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            Email = $"{username}@test.local",
            NormalizedEmail = $"{username}@test.local".ToUpperInvariant(),
            FirstName = username,
            LastName = "Test",
            LockoutEnabled = locked,
            LockoutEnd = locked ? DateTimeOffset.MaxValue : null,
        };
        Db.Users.Add(user);

        foreach (var teamId in teams ?? [])
        {
            Db.UserClaims.Add(new IdentityUserClaim<string>
            {
                UserId = user.Id,
                ClaimType = "team",
                ClaimValue = teamId.ToString(CultureInfo.InvariantCulture),
            });
        }

        await Db.SaveChangesAsync();
        return user;
    }

    private async Task<CourseEntity> SeedCourse()
    {
        var course = new CourseEntity { Name = "Test", Status = CourseStatus.Published };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        return course;
    }

    // ─── Backoffice ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetStats_BackOffice_ActiveLearnersCountsNonLockedUsers()
    {
        await SeedUser("alice");
        await SeedUser("bob");
        await SeedUser("locked", locked: true);
        _user.IsBackOffice.Returns(true);

        var result = await _sut.GetStats();

        Assert.That(result.Value!.ActiveLearners, Is.EqualTo(2));
    }

    [Test]
    public async Task GetStats_BackOffice_CompletedEnrollmentsCountsAllCompleted()
    {
        _user.IsBackOffice.Returns(true);
        var course = await SeedCourse();
        var alice = await SeedUser("alice");
        var bob = await SeedUser("bob");
        Db.Enrollments.AddRange(
            new EnrollmentEntity { UserId = alice.Id, CourseId = course.Id, Status = EnrollmentStatus.Completed },
            new EnrollmentEntity { UserId = bob.Id, CourseId = course.Id, Status = EnrollmentStatus.Completed },
            new EnrollmentEntity { UserId = alice.Id, CourseId = course.Id, Status = EnrollmentStatus.InProgress });
        await Db.SaveChangesAsync();

        var result = await _sut.GetStats();

        Assert.That(result.Value!.CompletedEnrollments, Is.EqualTo(2));
    }

    // ─── Manager ─────────────────────────────────────────────────────────────

    [Test]
    public async Task GetStats_Manager_ActiveLearnersCountsTeamMembers()
    {
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        await SeedUser("alice", teams: [team.Id]);
        await SeedUser("bob", teams: [team.Id]);
        await SeedUser("carol"); // not in team

        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { team.Id });

        var result = await _sut.GetStats();

        Assert.That(result.Value!.ActiveLearners, Is.EqualTo(2));
    }

    [Test]
    public async Task GetStats_Manager_CompletedEnrollmentsCountsTeamCompletions()
    {
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        var course = await SeedCourse();
        await Db.SaveChangesAsync();

        var alice = await SeedUser("alice", teams: [team.Id]);
        var carol = await SeedUser("carol"); // not in team
        Db.Enrollments.AddRange(
            new EnrollmentEntity { UserId = alice.Id, CourseId = course.Id, Status = EnrollmentStatus.Completed },
            new EnrollmentEntity { UserId = carol.Id, CourseId = course.Id, Status = EnrollmentStatus.Completed });
        await Db.SaveChangesAsync();

        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { team.Id });

        var result = await _sut.GetStats();

        Assert.That(result.Value!.CompletedEnrollments, Is.EqualTo(1));
    }

    // ─── Learner ──────────────────────────────────────────────────────────────

    [Test]
    public async Task GetStats_Learner_ActiveLearnersIsZero()
    {
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(Array.Empty<int>());

        var result = await _sut.GetStats();

        Assert.That(result.Value!.ActiveLearners, Is.EqualTo(0));
    }

    [Test]
    public async Task GetStats_Learner_CompletedEnrollmentsCountsOwnCompletions()
    {
        const string userId = "learner-1";
        _user.IsBackOffice.Returns(false);
        _user.UserId.Returns(userId);
        _user.Teams.Returns(Array.Empty<int>());

        var course = await SeedCourse();
        Db.Enrollments.AddRange(
            new EnrollmentEntity { UserId = userId, CourseId = course.Id, Status = EnrollmentStatus.Completed },
            new EnrollmentEntity { UserId = userId, CourseId = course.Id, Status = EnrollmentStatus.InProgress },
            new EnrollmentEntity { UserId = "other", CourseId = course.Id, Status = EnrollmentStatus.Completed });
        await Db.SaveChangesAsync();

        var result = await _sut.GetStats();

        Assert.That(result.Value!.CompletedEnrollments, Is.EqualTo(1));
    }
}
