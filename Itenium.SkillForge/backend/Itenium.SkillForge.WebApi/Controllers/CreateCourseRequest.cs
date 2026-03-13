using Itenium.SkillForge.Entities;

namespace Itenium.SkillForge.WebApi.Controllers;

public record CreateCourseRequest(
    string Name,
    string? Description,
    string? Category,
    string? Level,
    CourseStatus Status = CourseStatus.Draft,
    bool IsMandatory = false);
