using System.Globalization;
using System.Security.Claims;
using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Itenium.SkillForge.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi;

public class DashboardService : IDashboardService
{
    private static readonly TimeSpan InactiveThreshold = TimeSpan.FromDays(21);

    private readonly UserManager<ForgeUser> _userManager;
    private readonly AppDbContext _db;

    public DashboardService(UserManager<ForgeUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<IList<ConsultantSummaryDto>> GetConsultantSummariesAsync(ICollection<int> teamIds)
    {
        var found = new HashSet<ForgeUser>();
        foreach (var teamId in teamIds)
        {
            var claim = new Claim("team", teamId.ToString(CultureInfo.InvariantCulture));
            var users = await _userManager.GetUsersForClaimAsync(claim);
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("learner", StringComparer.Ordinal))
                {
                    found.Add(user);
                }
            }
        }

        var userIds = found.Select(u => u.Id).ToList();
        var activities = await _db.UserActivities
            .Where(a => userIds.Contains(a.UserId))
            .ToDictionaryAsync(a => a.UserId);

        var activeGoalCounts = await _db.Goals
            .Where(g => userIds.Contains(g.ConsultantId))
            .GroupBy(g => g.ConsultantId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        var now = DateTime.UtcNow;

        var readinessFlagsByUser = await _db.ReadinessFlags
            .Where(f => f.ResolvedAt == null && userIds.Contains(f.Goal.ConsultantId))
            .Select(f => new { f.Goal.ConsultantId, f.RaisedAt })
            .GroupBy(f => f.ConsultantId)
            .ToDictionaryAsync(
                g => g.Key,
                g => (int)(now - g.Min(f => f.RaisedAt)).TotalDays);

        var result = new List<ConsultantSummaryDto>();
        foreach (var user in found.OrderBy(u => u.LastName, StringComparer.Ordinal).ThenBy(u => u.FirstName, StringComparer.Ordinal))
        {
            var claims = await _userManager.GetClaimsAsync(user);
            var teams = claims
                .Where(c => c.Type == "team")
                .Select(c => int.Parse(c.Value, CultureInfo.InvariantCulture))
                .ToArray();

            activities.TryGetValue(user.Id, out var activity);
            var lastActivityAt = activity?.LastActivityAt;
            var isInactive = lastActivityAt == null || (now - lastActivityAt.Value) > InactiveThreshold;

            activeGoalCounts.TryGetValue(user.Id, out var activeGoalCount);
            var isReady = readinessFlagsByUser.TryGetValue(user.Id, out var flagAge);

            result.Add(new ConsultantSummaryDto(
                user.Id,
                user.FirstName ?? string.Empty,
                user.LastName ?? string.Empty,
                user.Email ?? string.Empty,
                teams,
                lastActivityAt,
                isInactive,
                ActiveGoalCount: activeGoalCount,
                IsReady: isReady,
                ReadinessFlagAgeInDays: isReady ? flagAge : null));
        }

        return result;
    }

    public async Task RecordActivityAsync(string userId)
    {
        var activity = await _db.UserActivities.FindAsync(userId);
        if (activity == null)
        {
            _db.UserActivities.Add(new UserActivityEntity { UserId = userId, LastActivityAt = DateTime.UtcNow });
        }
        else
        {
            activity.LastActivityAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }
}
