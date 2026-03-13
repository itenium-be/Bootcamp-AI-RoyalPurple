using System.Globalization;
using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class TeamControllerTests : DatabaseTestBase
{
    private ISkillForgeUser _user = null!;
    private TeamController _sut = null!;

    [SetUp]
    public void Setup()
    {
        _user = Substitute.For<ISkillForgeUser>();
        _sut = new TeamController(Db, _user);
    }

    [Test]
    public async Task GetUserTeams_WhenBackOffice_ReturnsAllTeams()
    {
        Db.Teams.AddRange(
            new TeamEntity { Name = "Java" },
            new TeamEntity { Name = ".NET" },
            new TeamEntity { Name = "QA" });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(true);

        var result = await _sut.GetUserTeams();

        var teams = result.Value!;
        Assert.That(teams, Has.Count.EqualTo(3));
        Assert.That(teams.Select(t => t.Name), Contains.Item("Java"));
        Assert.That(teams.Select(t => t.Name), Contains.Item(".NET"));
        Assert.That(teams.Select(t => t.Name), Contains.Item("QA"));
    }

    [Test]
    public async Task GetUserTeams_WhenNotBackOffice_ReturnsOnlyUserTeams()
    {
        var javaTeam = new TeamEntity { Name = "Java" };
        var dotnetTeam = new TeamEntity { Name = ".NET" };
        var qaTeam = new TeamEntity { Name = "QA" };
        Db.Teams.AddRange(javaTeam, dotnetTeam, qaTeam);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { javaTeam.Id, qaTeam.Id });

        var result = await _sut.GetUserTeams();

        var teams = result.Value!;
        Assert.That(teams, Has.Count.EqualTo(2));
        Assert.That(teams.Select(t => t.Name), Contains.Item("Java"));
        Assert.That(teams.Select(t => t.Name), Contains.Item("QA"));
        Assert.That(teams.Select(t => t.Name), Does.Not.Contain(".NET"));
    }

    [Test]
    public async Task GetUserTeams_WhenNotBackOfficeAndNoTeams_ReturnsEmpty()
    {
        Db.Teams.AddRange(
            new TeamEntity { Name = "Java" },
            new TeamEntity { Name = ".NET" });
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(Array.Empty<int>());

        var result = await _sut.GetUserTeams();

        var teams = result.Value!;
        Assert.That(teams, Is.Empty);
    }

    [Test]
    public async Task GetUserTeams_WhenUserHasNonExistentTeamId_IgnoresIt()
    {
        var javaTeam = new TeamEntity { Name = "Java" };
        Db.Teams.Add(javaTeam);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { javaTeam.Id, 999 });

        var result = await _sut.GetUserTeams();

        var teams = result.Value!;
        Assert.That(teams, Has.Count.EqualTo(1));
        Assert.That(teams.Select(t => t.Name), Contains.Item("Java"));
    }

    [Test]
    public async Task CreateTeam_BackOffice_AddsTeamAndReturnsCreated()
    {
        _user.IsBackOffice.Returns(true);

        var result = await _sut.CreateTeam(new TeamRequest("DevOps"));

        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var team = created!.Value as TeamEntity;
        Assert.That(team!.Name, Is.EqualTo("DevOps"));
        Assert.That(await Db.Teams.FindAsync(team.Id), Is.Not.Null);
    }

    [Test]
    public async Task CreateTeam_NonBackOffice_ReturnsForbid()
    {
        _user.IsBackOffice.Returns(false);

        var result = await _sut.CreateTeam(new TeamRequest("DevOps"));

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task UpdateTeam_BackOffice_UpdatesName()
    {
        var team = new TeamEntity { Name = "Old" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(true);

        var result = await _sut.UpdateTeam(team.Id, new TeamRequest("New"));

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var updated = ok!.Value as TeamEntity;
        Assert.That(updated!.Name, Is.EqualTo("New"));
    }

    [Test]
    public async Task UpdateTeam_WhenNotExists_ReturnsNotFound()
    {
        _user.IsBackOffice.Returns(true);

        var result = await _sut.UpdateTeam(999, new TeamRequest("New"));

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task DeleteTeam_BackOffice_RemovesTeam()
    {
        var team = new TeamEntity { Name = "ToDelete" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(true);

        var result = await _sut.DeleteTeam(team.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(await Db.Teams.FindAsync(team.Id), Is.Null);
    }

    [Test]
    public async Task DeleteTeam_WhenNotExists_ReturnsNotFound()
    {
        _user.IsBackOffice.Returns(true);

        var result = await _sut.DeleteTeam(999);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<ForgeUser> SeedUser(string username, int[]? teams = null)
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

    // ─── GetMembers ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetMembers_NonBackOffice_NotInTeam_ReturnsForbid()
    {
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(Array.Empty<int>());
        var result = await _sut.GetMembers(1);
        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task GetMembers_Manager_InTeam_ReturnsMembers()
    {
        _user.IsBackOffice.Returns(false);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        _user.Teams.Returns(new[] { team.Id });
        await SeedUser("alice", [team.Id]);

        var result = await _sut.GetMembers(team.Id);

        Assert.That(result.Value!, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetMembers_TeamNotFound_ReturnsNotFound()
    {
        _user.IsBackOffice.Returns(true);
        var result = await _sut.GetMembers(999);
        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task GetMembers_ReturnsUsersInTeam()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        await SeedUser("alice", [team.Id]);
        await SeedUser("bob");

        var result = await _sut.GetMembers(team.Id);

        Assert.That(result.Value!, Has.Count.EqualTo(1));
        Assert.That(result.Value![0].Username, Is.EqualTo("alice"));
    }

    // ─── AddMember ────────────────────────────────────────────────────────────

    [Test]
    public async Task AddMember_NonBackOffice_ReturnsForbid()
    {
        _user.IsBackOffice.Returns(false);
        var result = await _sut.AddMember(1, "user-1");
        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task AddMember_TeamNotFound_ReturnsNotFound()
    {
        _user.IsBackOffice.Returns(true);
        var result = await _sut.AddMember(999, "user-1");
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task AddMember_UserNotFound_ReturnsNotFound()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        var result = await _sut.AddMember(team.Id, "nonexistent");
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task AddMember_AlreadyMember_ReturnsBadRequest()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        var user = await SeedUser("alice", [team.Id]);

        var result = await _sut.AddMember(team.Id, user.Id);
        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task AddMember_BackOffice_AddsClaimAndReturnsNoContent()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        var user = await SeedUser("alice");

        var result = await _sut.AddMember(team.Id, user.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(Db.UserClaims.Any(c => c.UserId == user.Id && c.ClaimType == "team" && c.ClaimValue == team.Id.ToString(CultureInfo.InvariantCulture)), Is.True);
    }

    // ─── RemoveMember ─────────────────────────────────────────────────────────

    [Test]
    public async Task RemoveMember_NonBackOffice_ReturnsForbid()
    {
        _user.IsBackOffice.Returns(false);
        var result = await _sut.RemoveMember(1, "user-1");
        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task RemoveMember_TeamNotFound_ReturnsNotFound()
    {
        _user.IsBackOffice.Returns(true);
        var result = await _sut.RemoveMember(999, "user-1");
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task RemoveMember_NotMember_ReturnsNotFound()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        var user = await SeedUser("alice");

        var result = await _sut.RemoveMember(team.Id, user.Id);
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task RemoveMember_BackOffice_RemovesClaimAndReturnsNoContent()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        var user = await SeedUser("alice", [team.Id]);

        var result = await _sut.RemoveMember(team.Id, user.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(Db.UserClaims.Any(c => c.UserId == user.Id && c.ClaimType == "team"), Is.False);
    }

    // ─── GetTeamProgress ─────────────────────────────────────────────────────

    [Test]
    public async Task GetTeamProgress_NonBackOffice_NotInTeam_ReturnsForbid()
    {
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(Array.Empty<int>());

        var result = await _sut.GetTeamProgress(1);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task GetTeamProgress_TeamNotFound_ReturnsNotFound()
    {
        _user.IsBackOffice.Returns(true);

        var result = await _sut.GetTeamProgress(999);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task GetTeamProgress_Manager_InTeam_CanAccess()
    {
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { team.Id });

        var result = await _sut.GetTeamProgress(team.Id);

        Assert.That(result.Value, Is.Not.Null);
    }

    [Test]
    public async Task GetTeamProgress_ReturnsMembersWithEnrollments()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        var course = new CourseEntity { Name = "C# Basics", Status = CourseStatus.Published };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();

        var alice = await SeedUser("alice", [team.Id]);
        Db.Enrollments.Add(new EnrollmentEntity
        {
            UserId = alice.Id,
            CourseId = course.Id,
            Status = EnrollmentStatus.InProgress,
        });
        await Db.SaveChangesAsync();

        var result = await _sut.GetTeamProgress(team.Id);

        var members = result.Value!;
        Assert.That(members, Has.Count.EqualTo(1));
        Assert.That(members[0].FullName, Is.EqualTo("alice Test"));
        Assert.That(members[0].Enrollments, Has.Count.EqualTo(1));
        Assert.That(members[0].Enrollments[0].CourseName, Is.EqualTo("C# Basics"));
        Assert.That(members[0].Enrollments[0].Status, Is.EqualTo(EnrollmentStatus.InProgress));
    }

    [Test]
    public async Task GetTeamProgress_MemberWithNoEnrollments_IncludedWithEmptyList()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        await SeedUser("bob", [team.Id]);

        var result = await _sut.GetTeamProgress(team.Id);

        var members = result.Value!;
        Assert.That(members, Has.Count.EqualTo(1));
        Assert.That(members[0].Enrollments, Is.Empty);
    }

    // ─── GetTeamAssignments ───────────────────────────────────────────────────

    [Test]
    public async Task GetTeamAssignments_NonBackOffice_NotInTeam_ReturnsForbid()
    {
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(Array.Empty<int>());

        var result = await _sut.GetTeamAssignments(1);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task GetTeamAssignments_TeamNotFound_ReturnsNotFound()
    {
        _user.IsBackOffice.Returns(true);

        var result = await _sut.GetTeamAssignments(999);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task GetTeamAssignments_Manager_InTeam_CanAccess()
    {
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { team.Id });

        var result = await _sut.GetTeamAssignments(team.Id);

        Assert.That(result.Value, Is.Not.Null);
    }

    [Test]
    public async Task GetTeamAssignments_ReturnsAssignmentsWithCourseDetails()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        var course = new CourseEntity { Name = "C# Basics", Status = CourseStatus.Published };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        Db.TeamAssignments.Add(new TeamAssignmentEntity
        {
            TeamId = team.Id,
            CourseId = course.Id,
            IsMandatory = true,
        });
        await Db.SaveChangesAsync();

        var result = await _sut.GetTeamAssignments(team.Id);

        var assignments = result.Value!;
        Assert.That(assignments, Has.Count.EqualTo(1));
        Assert.That(assignments[0].CourseName, Is.EqualTo("C# Basics"));
        Assert.That(assignments[0].IsMandatory, Is.True);
    }

    // ─── AssignCourse ─────────────────────────────────────────────────────────

    [Test]
    public async Task AssignCourse_NonBackOffice_NotInTeam_ReturnsForbid()
    {
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(Array.Empty<int>());

        var result = await _sut.AssignCourse(1, 1, new AssignCourseRequest(true));

        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task AssignCourse_TeamNotFound_ReturnsNotFound()
    {
        _user.IsBackOffice.Returns(true);

        var result = await _sut.AssignCourse(999, 1, new AssignCourseRequest(true));

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task AssignCourse_CourseNotFound_ReturnsNotFound()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        var result = await _sut.AssignCourse(team.Id, 999, new AssignCourseRequest(true));

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task AssignCourse_AlreadyAssigned_ReturnsBadRequest()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        var course = new CourseEntity { Name = "C# Basics", Status = CourseStatus.Published };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        Db.TeamAssignments.Add(new TeamAssignmentEntity { TeamId = team.Id, CourseId = course.Id, IsMandatory = true });
        await Db.SaveChangesAsync();

        var result = await _sut.AssignCourse(team.Id, course.Id, new AssignCourseRequest(true));

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task AssignCourse_Manager_InTeam_CreatesAssignment()
    {
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        var course = new CourseEntity { Name = "C# Basics", Status = CourseStatus.Published };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(new[] { team.Id });

        var result = await _sut.AssignCourse(team.Id, course.Id, new AssignCourseRequest(false));

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(Db.TeamAssignments.Any(a => a.TeamId == team.Id && a.CourseId == course.Id), Is.True);
    }

    // ─── UnassignCourse ───────────────────────────────────────────────────────

    [Test]
    public async Task UnassignCourse_NonBackOffice_NotInTeam_ReturnsForbid()
    {
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(Array.Empty<int>());

        var result = await _sut.UnassignCourse(1, 1);

        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task UnassignCourse_NotAssigned_ReturnsNotFound()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        var result = await _sut.UnassignCourse(team.Id, 999);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task UnassignCourse_BackOffice_RemovesAssignment()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        var course = new CourseEntity { Name = "C# Basics", Status = CourseStatus.Published };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        Db.TeamAssignments.Add(new TeamAssignmentEntity { TeamId = team.Id, CourseId = course.Id, IsMandatory = true });
        await Db.SaveChangesAsync();

        var result = await _sut.UnassignCourse(team.Id, course.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(Db.TeamAssignments.Any(a => a.TeamId == team.Id && a.CourseId == course.Id), Is.False);
    }

    // ─── UpdateAssignment ─────────────────────────────────────────────────────

    [Test]
    public async Task UpdateAssignment_NonBackOffice_NotInTeam_ReturnsForbid()
    {
        _user.IsBackOffice.Returns(false);
        _user.Teams.Returns(Array.Empty<int>());

        var result = await _sut.UpdateAssignment(1, 1, new AssignCourseRequest(true));

        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task UpdateAssignment_NotAssigned_ReturnsNotFound()
    {
        _user.IsBackOffice.Returns(true);

        var result = await _sut.UpdateAssignment(1, 999, new AssignCourseRequest(true));

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task UpdateAssignment_BackOffice_UpdatesIsMandatory()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        var course = new CourseEntity { Name = "C# Basics", Status = CourseStatus.Published };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        Db.TeamAssignments.Add(new TeamAssignmentEntity { TeamId = team.Id, CourseId = course.Id, IsMandatory = false });
        await Db.SaveChangesAsync();

        var result = await _sut.UpdateAssignment(team.Id, course.Id, new AssignCourseRequest(true));

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(Db.TeamAssignments.First(a => a.TeamId == team.Id && a.CourseId == course.Id).IsMandatory, Is.True);
    }

    // ─── Individual member assignments ────────────────────────────────────────

    [Test]
    public async Task AssignCourse_ToIndividualMember_CreatesUserAssignment()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        var course = new CourseEntity { Name = "C# Basics", Status = CourseStatus.Published };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        var alice = await SeedUser("alice", [team.Id]);

        var result = await _sut.AssignCourse(team.Id, course.Id, new AssignCourseRequest(true, alice.Id));

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(
            Db.TeamAssignments.Any(a => a.TeamId == team.Id && a.CourseId == course.Id && a.UserId == alice.Id),
            Is.True);
    }

    [Test]
    public async Task AssignCourse_TeamAndIndividualCanCoexist()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        var course = new CourseEntity { Name = "C# Basics", Status = CourseStatus.Published };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        var alice = await SeedUser("alice", [team.Id]);
        // Team-wide assignment
        Db.TeamAssignments.Add(new TeamAssignmentEntity { TeamId = team.Id, CourseId = course.Id, IsMandatory = false });
        await Db.SaveChangesAsync();

        // Individual assignment for same course is allowed
        var result = await _sut.AssignCourse(team.Id, course.Id, new AssignCourseRequest(true, alice.Id));

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(Db.TeamAssignments.Count(a => a.TeamId == team.Id && a.CourseId == course.Id), Is.EqualTo(2));
    }

    [Test]
    public async Task AssignCourse_DuplicateIndividualAssignment_ReturnsBadRequest()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        var course = new CourseEntity { Name = "C# Basics", Status = CourseStatus.Published };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        var alice = await SeedUser("alice", [team.Id]);
        Db.TeamAssignments.Add(new TeamAssignmentEntity { TeamId = team.Id, CourseId = course.Id, UserId = alice.Id, IsMandatory = false });
        await Db.SaveChangesAsync();

        var result = await _sut.AssignCourse(team.Id, course.Id, new AssignCourseRequest(true, alice.Id));

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UnassignCourse_IndividualAssignment_RemovesOnlyThatRow()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        var course = new CourseEntity { Name = "C# Basics", Status = CourseStatus.Published };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        var alice = await SeedUser("alice", [team.Id]);
        // Both team-wide and individual
        Db.TeamAssignments.AddRange(
            new TeamAssignmentEntity { TeamId = team.Id, CourseId = course.Id, IsMandatory = false },
            new TeamAssignmentEntity { TeamId = team.Id, CourseId = course.Id, UserId = alice.Id, IsMandatory = true });
        await Db.SaveChangesAsync();

        var result = await _sut.UnassignCourse(team.Id, course.Id, alice.Id);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        // Team-wide assignment still exists
        Assert.That(Db.TeamAssignments.Any(a => a.TeamId == team.Id && a.CourseId == course.Id && a.UserId == null), Is.True);
        // Individual assignment is gone
        Assert.That(Db.TeamAssignments.Any(a => a.TeamId == team.Id && a.CourseId == course.Id && a.UserId == alice.Id), Is.False);
    }

    [Test]
    public async Task GetTeamAssignments_IncludesUserFullNameForIndividualAssignments()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        var course = new CourseEntity { Name = "C# Basics", Status = CourseStatus.Published };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        var alice = await SeedUser("alice", [team.Id]);
        Db.TeamAssignments.Add(new TeamAssignmentEntity { TeamId = team.Id, CourseId = course.Id, UserId = alice.Id, IsMandatory = true });
        await Db.SaveChangesAsync();

        var result = await _sut.GetTeamAssignments(team.Id);

        var assignments = result.Value!;
        Assert.That(assignments, Has.Count.EqualTo(1));
        Assert.That(assignments[0].UserId, Is.EqualTo(alice.Id));
        Assert.That(assignments[0].UserFullName, Is.EqualTo("alice Test"));
    }

    [Test]
    public async Task GetTeamAssignments_TeamWideAssignment_HasNullUserFields()
    {
        _user.IsBackOffice.Returns(true);
        var team = new TeamEntity { Name = "Java" };
        Db.Teams.Add(team);
        var course = new CourseEntity { Name = "C# Basics", Status = CourseStatus.Published };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync();
        Db.TeamAssignments.Add(new TeamAssignmentEntity { TeamId = team.Id, CourseId = course.Id, IsMandatory = false });
        await Db.SaveChangesAsync();

        var result = await _sut.GetTeamAssignments(team.Id);

        var assignments = result.Value!;
        Assert.That(assignments[0].UserId, Is.Null);
        Assert.That(assignments[0].UserFullName, Is.Null);
    }
}
