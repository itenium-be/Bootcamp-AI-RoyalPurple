namespace Itenium.SkillForge.WebApi.Controllers;

public record TeamAssignmentDto(int CourseId, string CourseName, bool IsMandatory, DateTime AssignedAt, string? UserId = null, string? UserFullName = null);

public record AssignCourseRequest(bool IsMandatory, string? UserId = null);
