using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Entities;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.Data;

public class AppDbContext : ForgeIdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<TeamEntity> Teams => Set<TeamEntity>();

    public DbSet<CourseEntity> Courses => Set<CourseEntity>();

    public DbSet<EnrollmentEntity> Enrollments => Set<EnrollmentEntity>();

    public DbSet<FeedbackEntity> Feedback => Set<FeedbackEntity>();

    public DbSet<LoginHistoryEntity> LoginHistory => Set<LoginHistoryEntity>();

    public DbSet<TeamAssignmentEntity> TeamAssignments => Set<TeamAssignmentEntity>();

    public DbSet<CourseSuggestionEntity> CourseSuggestions => Set<CourseSuggestionEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}
