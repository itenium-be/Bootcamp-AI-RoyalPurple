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

    public DbSet<SkillCategoryEntity> SkillCategories => Set<SkillCategoryEntity>();

    public DbSet<SkillEntity> Skills => Set<SkillEntity>();

    public DbSet<UserActivityEntity> UserActivities => Set<UserActivityEntity>();

    public DbSet<SkillLevelEntity> SkillLevels => Set<SkillLevelEntity>();

    public DbSet<SkillPrerequisiteEntity> SkillPrerequisites => Set<SkillPrerequisiteEntity>();

    public DbSet<FeedbackEntity> Feedbacks => Set<FeedbackEntity>();

    public DbSet<FeedbackCommentEntity> FeedbackComments => Set<FeedbackCommentEntity>();

    public DbSet<ConsultantProfileEntity> ConsultantProfiles => Set<ConsultantProfileEntity>();

    public DbSet<CourseResourceEntity> CourseResources => Set<CourseResourceEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<SkillPrerequisiteEntity>()
            .HasKey(sp => new { sp.SkillId, sp.PrerequisiteSkillId });

        builder.Entity<SkillPrerequisiteEntity>()
            .HasOne(sp => sp.Skill)
            .WithMany(s => s.Prerequisites)
            .HasForeignKey(sp => sp.SkillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SkillPrerequisiteEntity>()
            .HasOne(sp => sp.PrerequisiteSkill)
            .WithMany()
            .HasForeignKey(sp => sp.PrerequisiteSkillId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
