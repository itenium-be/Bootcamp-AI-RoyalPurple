using Itenium.SkillForge.Services;
using Itenium.SkillForge.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class DashboardControllerTests
{
    private IDashboardService _dashboardService = null!;
    private ISkillForgeUser _currentUser = null!;
    private DashboardController _sut = null!;

    private static readonly int[] TeamIds1 = [1];
    private static readonly int[] TeamIds12 = [1, 2];

    private static ConsultantSummaryDto ActiveConsultant(string id = "id1") =>
        new(id, "Alice", "Smith", "alice@test.local", [1], DateTime.UtcNow.AddDays(-1), false, 0, false, null);

    private static ConsultantSummaryDto InactiveConsultant(string id = "id2") =>
        new(id, "Bob", "Jones", "bob@test.local", [1], DateTime.UtcNow.AddDays(-30), true, 0, false, null);

    private static ConsultantSummaryDto NeverActiveConsultant(string id = "id3") =>
        new(id, "Charlie", "Brown", "charlie@test.local", [1], null, true, 0, false, null);

    [SetUp]
    public void Setup()
    {
        _dashboardService = Substitute.For<IDashboardService>();
        _currentUser = Substitute.For<ISkillForgeUser>();
        _sut = new DashboardController(_dashboardService, _currentUser);
    }

    // GET /api/dashboard

    [Test]
    public async Task GetDashboard_ReturnsConsultantSummariesForCoachTeams()
    {
        IList<ConsultantSummaryDto> summaries = [ActiveConsultant(), InactiveConsultant()];
        _currentUser.Teams.Returns(TeamIds1);
        _dashboardService.GetConsultantSummariesAsync(TeamIds1).Returns(summaries);

        var result = await _sut.GetDashboard();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value as IList<ConsultantSummaryDto>, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetDashboard_WithNoTeams_ReturnsEmpty()
    {
        _currentUser.Teams.Returns(Array.Empty<int>());
        _dashboardService.GetConsultantSummariesAsync([]).Returns([]);

        var result = await _sut.GetDashboard();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value as IList<ConsultantSummaryDto>, Is.Empty);
    }

    [Test]
    public async Task GetDashboard_InactiveConsultant_HasIsInactiveTrue()
    {
        IList<ConsultantSummaryDto> summaries = [InactiveConsultant()];
        _currentUser.Teams.Returns(TeamIds1);
        _dashboardService.GetConsultantSummariesAsync(TeamIds1).Returns(summaries);

        var result = await _sut.GetDashboard();

        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as IList<ConsultantSummaryDto>;
        Assert.That(list![0].IsInactive, Is.True);
    }

    [Test]
    public async Task GetDashboard_ActiveConsultant_HasIsInactiveFalse()
    {
        IList<ConsultantSummaryDto> summaries = [ActiveConsultant()];
        _currentUser.Teams.Returns(TeamIds1);
        _dashboardService.GetConsultantSummariesAsync(TeamIds1).Returns(summaries);

        var result = await _sut.GetDashboard();

        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as IList<ConsultantSummaryDto>;
        Assert.That(list![0].IsInactive, Is.False);
    }

    [Test]
    public async Task GetDashboard_NeverActiveConsultant_HasIsInactiveTrue()
    {
        IList<ConsultantSummaryDto> summaries = [NeverActiveConsultant()];
        _currentUser.Teams.Returns(TeamIds1);
        _dashboardService.GetConsultantSummariesAsync(TeamIds1).Returns(summaries);

        var result = await _sut.GetDashboard();

        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as IList<ConsultantSummaryDto>;
        Assert.That(list![0].IsInactive, Is.True);
    }

    [Test]
    public async Task GetDashboard_ConsultantsStubbedWithZeroGoals()
    {
        IList<ConsultantSummaryDto> summaries = [ActiveConsultant()];
        _currentUser.Teams.Returns(TeamIds1);
        _dashboardService.GetConsultantSummariesAsync(TeamIds1).Returns(summaries);

        var result = await _sut.GetDashboard();

        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as IList<ConsultantSummaryDto>;
        Assert.That(list![0].ActiveGoalCount, Is.EqualTo(0));
    }

    [Test]
    public async Task GetDashboard_ReadyConsultant_HasIsReadyTrueAndFlagAge()
    {
        IList<ConsultantSummaryDto> summaries =
        [
            new("id1", "Alice", "Smith", "alice@test.local", [1], DateTime.UtcNow.AddDays(-1), false, 1, true, 2),
        ];
        _currentUser.Teams.Returns(TeamIds1);
        _dashboardService.GetConsultantSummariesAsync(TeamIds1).Returns(summaries);

        var result = await _sut.GetDashboard();

        var ok = result.Result as OkObjectResult;
        var list = ok!.Value as IList<ConsultantSummaryDto>;
        Assert.That(list![0].IsReady, Is.True);
        Assert.That(list[0].ReadinessFlagAgeInDays, Is.EqualTo(2));
    }

    [Test]
    public async Task GetDashboard_PassesAllTeamIdsToService()
    {
        _currentUser.Teams.Returns(TeamIds12);
        _dashboardService.GetConsultantSummariesAsync(TeamIds12).Returns([]);

        await _sut.GetDashboard();

        await _dashboardService.Received(1).GetConsultantSummariesAsync(TeamIds12);
    }

    // POST /api/dashboard/activity

    [Test]
    public async Task RecordActivity_CallsServiceWithCurrentUserId()
    {
        _currentUser.UserId.Returns("user123");

        var result = await _sut.RecordActivity();

        await _dashboardService.Received(1).RecordActivityAsync("user123");
        Assert.That(result, Is.TypeOf<NoContentResult>());
    }
}
