namespace Itenium.SkillForge.WebApi.Controllers;

public record GoalResourceDto(int Id, string Title, string Url, string Type);

public record GoalDto(
    int Id,
    string SkillName,
    int CurrentLevel,
    int TargetLevel,
    DateTime Deadline,
    IReadOnlyList<GoalResourceDto> Resources,
    DateTime? ReadinessFlagRaisedAt,
    int? ReadinessFlagAgeDays);

public record ReadinessFlagDto(int GoalId, string SkillName, string ConsultantId, DateTime RaisedAt, int AgeDays);

public record CreateGoalResourceRequest(string Title, string Url, string Type);

public record CreateGoalRequest(
    string ConsultantId,
    int SkillId,
    int CurrentLevel,
    int TargetLevel,
    DateTime Deadline,
    IReadOnlyList<CreateGoalResourceRequest> Resources);
