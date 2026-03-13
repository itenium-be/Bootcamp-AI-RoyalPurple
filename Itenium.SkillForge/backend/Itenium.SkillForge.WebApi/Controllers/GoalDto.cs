namespace Itenium.SkillForge.WebApi.Controllers;

public record GoalResourceDto(int Id, string Title, string Url, string Type);

public record GoalDto(
    int Id,
    string SkillName,
    int CurrentLevel,
    int TargetLevel,
    DateTime Deadline,
    IReadOnlyList<GoalResourceDto> Resources,
    bool HasActiveReadinessFlag);

public record CreateGoalResourceRequest(string Title, string Url, string Type);

public record CreateGoalRequest(
    string ConsultantId,
    int SkillId,
    int CurrentLevel,
    int TargetLevel,
    DateTime Deadline,
    IReadOnlyList<CreateGoalResourceRequest> Resources);
