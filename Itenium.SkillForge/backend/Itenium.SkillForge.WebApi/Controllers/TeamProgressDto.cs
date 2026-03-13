using Itenium.SkillForge.Entities;

namespace Itenium.SkillForge.WebApi.Controllers;

public record TeamMemberProgressDto(
    string UserId,
    string FullName,
    string Email,
    IList<EnrollmentProgressDto> Enrollments);

public record EnrollmentProgressDto(
    int CourseId,
    string CourseName,
    EnrollmentStatus Status,
    DateTime EnrolledAt,
    DateTime? CompletedAt);
