namespace Itenium.SkillForge.Services;

public record ConsultantSummaryDto(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    int[] Teams,
    DateTime? LastActivityAt,
    bool IsInactive,
    int ActiveGoalCount,
    bool IsReady,
    int? ReadinessFlagAgeInDays);

public interface IDashboardService
{
    /// <summary>
    /// Returns a summary of all consultants in the given teams,
    /// enriched with activity, goal, and readiness data.
    /// </summary>
    Task<IList<ConsultantSummaryDto>> GetConsultantSummariesAsync(ICollection<int> teamIds);

    /// <summary>
    /// Records that the given user was active right now.
    /// </summary>
    Task RecordActivityAsync(string userId);
}
