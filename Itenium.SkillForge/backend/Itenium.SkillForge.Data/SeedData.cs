using System.Security.Claims;
using Itenium.Forge.Security.OpenIddict;
using Itenium.SkillForge.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Itenium.SkillForge.Data;

public static class SeedData
{
    public static async Task SeedDevelopmentData(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SeedTeams(db);
        await SeedCourses(db);
        await SeedSkills(db);
        await app.SeedTestUsers();
    }

    private static async Task SeedTeams(AppDbContext db)
    {
        if (!await db.Teams.AnyAsync())
        {
            db.Teams.AddRange(
                new TeamEntity { Id = 1, Name = "Java" },
                new TeamEntity { Id = 2, Name = ".NET" },
                new TeamEntity { Id = 3, Name = "PO & Analysis" },
                new TeamEntity { Id = 4, Name = "QA" });
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedCourses(AppDbContext db)
    {
        if (!await db.Courses.AnyAsync())
        {
            db.Courses.AddRange(
                new CourseEntity { Id = 1, Name = "Introduction to Programming", Description = "Learn the basics of programming", Category = "Development", Level = "Beginner" },
                new CourseEntity { Id = 2, Name = "Advanced C#", Description = "Master C# programming language", Category = "Development", Level = "Advanced" },
                new CourseEntity { Id = 3, Name = "Cloud Architecture", Description = "Design scalable cloud solutions", Category = "Architecture", Level = "Intermediate" },
                new CourseEntity { Id = 4, Name = "Agile Project Management", Description = "Learn agile methodologies", Category = "Management", Level = "Beginner" });
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedSkills(AppDbContext db)
    {
        if (await db.Skills.AnyAsync())
            return;

        // Java team (id=1)
        db.Skills.AddRange(
            new SkillEntity { TeamId = 1, Tier = 1, Name = "Java Basics", Description = "Core Java syntax, types, and control flow" },
            new SkillEntity { TeamId = 1, Tier = 1, Name = "OOP Fundamentals", Description = "Classes, inheritance, polymorphism, encapsulation" },
            new SkillEntity { TeamId = 1, Tier = 1, Name = "Git & Version Control", Description = "Branching, merging, pull requests" },
            new SkillEntity { TeamId = 1, Tier = 1, Name = "Linux Basics", Description = "Command line, file system, shell scripting" },
            new SkillEntity { TeamId = 1, Tier = 2, Name = "Spring Boot", Description = "Building REST APIs with Spring Boot" },
            new SkillEntity { TeamId = 1, Tier = 2, Name = "SQL & Databases", Description = "Relational databases, queries, transactions" },
            new SkillEntity { TeamId = 1, Tier = 2, Name = "Unit Testing with JUnit", Description = "Writing and running unit tests" },
            new SkillEntity { TeamId = 1, Tier = 2, Name = "Maven / Gradle", Description = "Build tools and dependency management" },
            new SkillEntity { TeamId = 1, Tier = 3, Name = "Microservices", Description = "Designing and building microservice architectures" },
            new SkillEntity { TeamId = 1, Tier = 3, Name = "Docker & Kubernetes", Description = "Containerisation and orchestration" },
            new SkillEntity { TeamId = 1, Tier = 3, Name = "CI/CD Pipelines", Description = "Automated build, test, and deploy pipelines" },
            new SkillEntity { TeamId = 1, Tier = 4, Name = "System Design", Description = "Designing scalable, reliable distributed systems" },
            new SkillEntity { TeamId = 1, Tier = 4, Name = "Performance Tuning", Description = "Profiling and optimising JVM applications" });

        // .NET team (id=2)
        db.Skills.AddRange(
            new SkillEntity { TeamId = 2, Tier = 1, Name = "C# Fundamentals", Description = "Core C# syntax, types, and control flow" },
            new SkillEntity { TeamId = 2, Tier = 1, Name = "OOP Fundamentals", Description = "Classes, inheritance, polymorphism, encapsulation" },
            new SkillEntity { TeamId = 2, Tier = 1, Name = "Git & Version Control", Description = "Branching, merging, pull requests" },
            new SkillEntity { TeamId = 2, Tier = 1, Name = "Linux Basics", Description = "Command line, file system, shell scripting" },
            new SkillEntity { TeamId = 2, Tier = 2, Name = "ASP.NET Core", Description = "Building REST APIs with ASP.NET Core" },
            new SkillEntity { TeamId = 2, Tier = 2, Name = "Entity Framework Core", Description = "ORM, migrations, and database querying" },
            new SkillEntity { TeamId = 2, Tier = 2, Name = "Unit Testing with NUnit", Description = "Writing and running unit tests" },
            new SkillEntity { TeamId = 2, Tier = 2, Name = "NuGet & MSBuild", Description = "Package management and build tooling" },
            new SkillEntity { TeamId = 2, Tier = 3, Name = "Microservices", Description = "Designing and building microservice architectures" },
            new SkillEntity { TeamId = 2, Tier = 3, Name = "Docker & Kubernetes", Description = "Containerisation and orchestration" },
            new SkillEntity { TeamId = 2, Tier = 3, Name = "CI/CD Pipelines", Description = "Automated build, test, and deploy pipelines" },
            new SkillEntity { TeamId = 2, Tier = 4, Name = "System Design", Description = "Designing scalable, reliable distributed systems" },
            new SkillEntity { TeamId = 2, Tier = 4, Name = "Performance Tuning", Description = "Profiling and optimising .NET applications" });

        // PO & Analysis team (id=3)
        db.Skills.AddRange(
            new SkillEntity { TeamId = 3, Tier = 1, Name = "Agile Fundamentals", Description = "Scrum, Kanban, and agile values" },
            new SkillEntity { TeamId = 3, Tier = 1, Name = "User Story Writing", Description = "Writing clear, testable user stories" },
            new SkillEntity { TeamId = 3, Tier = 1, Name = "Requirements Gathering", Description = "Eliciting and documenting requirements" },
            new SkillEntity { TeamId = 3, Tier = 1, Name = "Stakeholder Management", Description = "Identifying and engaging stakeholders" },
            new SkillEntity { TeamId = 3, Tier = 2, Name = "Product Backlog Management", Description = "Prioritisation, grooming, and refinement" },
            new SkillEntity { TeamId = 3, Tier = 2, Name = "User Research", Description = "Interviews, surveys, and usability testing" },
            new SkillEntity { TeamId = 3, Tier = 2, Name = "Process Modelling", Description = "BPMN, use cases, and flow diagrams" },
            new SkillEntity { TeamId = 3, Tier = 2, Name = "Acceptance Criteria", Description = "BDD-style criteria and test scenarios" },
            new SkillEntity { TeamId = 3, Tier = 3, Name = "Product Strategy", Description = "Vision, roadmap planning, and OKRs" },
            new SkillEntity { TeamId = 3, Tier = 3, Name = "Data-Driven Decisions", Description = "Metrics, KPIs, and analytics" },
            new SkillEntity { TeamId = 3, Tier = 4, Name = "Digital Transformation", Description = "Leading organisational change with technology" });

        // QA team (id=4)
        db.Skills.AddRange(
            new SkillEntity { TeamId = 4, Tier = 1, Name = "Testing Fundamentals", Description = "Testing principles, types, and techniques" },
            new SkillEntity { TeamId = 4, Tier = 1, Name = "Test Case Design", Description = "Equivalence partitioning, boundary value analysis" },
            new SkillEntity { TeamId = 4, Tier = 1, Name = "Bug Reporting", Description = "Writing clear, reproducible bug reports" },
            new SkillEntity { TeamId = 4, Tier = 1, Name = "Agile Testing", Description = "Testing in sprint, shift-left testing" },
            new SkillEntity { TeamId = 4, Tier = 2, Name = "Test Automation Basics", Description = "Introduction to automated testing frameworks" },
            new SkillEntity { TeamId = 4, Tier = 2, Name = "Selenium / Playwright", Description = "Browser automation for end-to-end tests" },
            new SkillEntity { TeamId = 4, Tier = 2, Name = "API Testing", Description = "REST API testing with Postman and code-based tools" },
            new SkillEntity { TeamId = 4, Tier = 2, Name = "Performance Testing", Description = "Load and stress testing fundamentals" },
            new SkillEntity { TeamId = 4, Tier = 3, Name = "CI/CD Testing Integration", Description = "Embedding tests in automated pipelines" },
            new SkillEntity { TeamId = 4, Tier = 3, Name = "Security Testing", Description = "OWASP Top 10 and vulnerability scanning" },
            new SkillEntity { TeamId = 4, Tier = 4, Name = "QA Architecture", Description = "Designing test strategies for large-scale systems" });

        await db.SaveChangesAsync();
    }

    private static async Task SeedTestUsers(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ForgeUser>>();

        // BackOffice admin - no team claim (manages all)
        if (await userManager.FindByEmailAsync("backoffice@test.local") == null)
        {
            var admin = new ForgeUser
            {
                UserName = "backoffice",
                Email = "backoffice@test.local",
                EmailConfirmed = true,
                FirstName = "BackOffice",
                LastName = "Admin"
            };
            var result = await userManager.CreateAsync(admin, "AdminPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRolesAsync(admin, ["backoffice"]);
            }
        }

        // Local user for Java team only
        if (await userManager.FindByEmailAsync("java@test.local") == null)
        {
            var user = new ForgeUser
            {
                UserName = "java",
                Email = "java@test.local",
                EmailConfirmed = true,
                FirstName = "Java",
                LastName = "Developer"
            };
            var result = await userManager.CreateAsync(user, "UserPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "manager");
                await userManager.AddClaimAsync(user, new Claim("team", "1")); // Java
            }
        }

        // Local user for .NET team only
        if (await userManager.FindByEmailAsync("dotnet@test.local") == null)
        {
            var user = new ForgeUser
            {
                UserName = "dotnet",
                Email = "dotnet@test.local",
                EmailConfirmed = true,
                FirstName = "DotNet",
                LastName = "Developer"
            };
            var result = await userManager.CreateAsync(user, "UserPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "manager");
                await userManager.AddClaimAsync(user, new Claim("team", "2")); // .NET
            }
        }

        // User with access to multiple teams (Java + .NET)
        if (await userManager.FindByEmailAsync("multi@test.local") == null)
        {
            var user = new ForgeUser
            {
                UserName = "multi",
                Email = "multi@test.local",
                EmailConfirmed = true,
                FirstName = "Multi",
                LastName = "Team"
            };
            var result = await userManager.CreateAsync(user, "UserPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "manager");
                await userManager.AddClaimAsync(user, new Claim("team", "1")); // Java
                await userManager.AddClaimAsync(user, new Claim("team", "2")); // .NET
            }
        }

        // Learner user - basic learner role
        if (await userManager.FindByEmailAsync("learner@test.local") == null)
        {
            var user = new ForgeUser
            {
                UserName = "learner",
                Email = "learner@test.local",
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "Learner"
            };
            var result = await userManager.CreateAsync(user, "UserPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "learner");
                await userManager.AddClaimAsync(user, new Claim("team", "1")); // Java
            }
        }
    }
}
