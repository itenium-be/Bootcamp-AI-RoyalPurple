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
        await SeedCourseResources(db);
        await SeedSkillCatalogue(db);
        await app.SeedTestUsers();
        await SeedConsultantProfiles(db);
    }

    public static async Task SeedDevelopmentData_ForTest(AppDbContext db)
    {
        await SeedTeams(db);
        await SeedCourses(db);
        await SeedCourseResources(db);
        await SeedSkillCatalogue(db);
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
            await db.Database.ExecuteSqlRawAsync("SELECT setval('\"Teams_Id_seq\"', (SELECT MAX(\"Id\") FROM \"Teams\"))");
        }
    }

    private static async Task SeedCourses(AppDbContext db)
    {
        if (!await db.Courses.AnyAsync())
        {
            db.Courses.AddRange(
                new CourseEntity { Id = 1, Name = "Introduction to Programming", Description = "Learn the basics of programming", Category = "Development", Level = "Beginner" },
                new CourseEntity { Id = 2, Name = "Advanced C#", Description = "Master C# programming language", Category = "Development", Level = "Advanced", TeamId = 2 },        // .NET
                new CourseEntity { Id = 3, Name = "Cloud Architecture", Description = "Design scalable cloud solutions", Category = "Architecture", Level = "Intermediate", TeamId = 2 }, // .NET
                new CourseEntity { Id = 4, Name = "Agile Project Management", Description = "Learn agile methodologies", Category = "Management", Level = "Beginner", TeamId = 3 },        // PO & Analysis
                new CourseEntity { Id = 5, Name = "Spring Boot Fundamentals", Description = "Build Java applications with Spring Boot", Category = "Development", Level = "Intermediate", TeamId = 1 }, // Java
                new CourseEntity { Id = 6, Name = "Test Automation", Description = "Automate tests with Selenium and Playwright", Category = "Quality", Level = "Intermediate", TeamId = 4 });           // QA
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync("SELECT setval('\"Courses_Id_seq\"', (SELECT MAX(\"Id\") FROM \"Courses\"))");
        }
    }

    private static async Task SeedCourseResources(AppDbContext db)
    {
        if (await db.CourseResources.AnyAsync())
            return;

        var courseIds = await db.Courses.OrderBy(c => c.Id).Select(c => c.Id).ToListAsync();
        if (courseIds.Count == 0)
            return;

        var resources = new List<CourseResourceEntity>();
        var definitions = new[]
        {
            (Title: "Programming Basics Video Series", Type: CourseResourceType.Video, Url: (string?)"https://example.com/prog-basics", DurationMinutes: (int?)120, Order: 1, Description: (string?)"Introduction to core programming concepts"),
            (Title: "Hello World Exercises", Type: CourseResourceType.Exercise, Url: (string?)null, DurationMinutes: (int?)null, Order: 2, Description: (string?)"Hands-on exercises to get started"),
            (Title: "Clean Code Book", Type: CourseResourceType.Book, Url: (string?)null, DurationMinutes: (int?)null, Order: 3, Description: (string?)"Robert C. Martin's classic on writing clean code"),
            (Title: "Async/Await Deep Dive", Type: CourseResourceType.Article, Url: (string?)"https://example.com/async-await", DurationMinutes: (int?)null, Order: 1, Description: (string?)"Understanding async programming in .NET"),
            (Title: "LINQ Mastery Workshop", Type: CourseResourceType.Exercise, Url: (string?)null, DurationMinutes: (int?)90, Order: 2, Description: (string?)"Advanced LINQ query exercises"),
            (Title: "Microservices Architecture Patterns", Type: CourseResourceType.Video, Url: (string?)"https://example.com/microservices", DurationMinutes: (int?)180, Order: 1, Description: (string?)null),
            (Title: "Scrum Guide", Type: CourseResourceType.Article, Url: (string?)"https://scrumguides.org", DurationMinutes: (int?)null, Order: 1, Description: (string?)"The official Scrum Guide"),
        };

        for (var i = 0; i < Math.Min(courseIds.Count, definitions.Length); i++)
        {
            var def = definitions[i];
            resources.Add(new CourseResourceEntity
            {
                CourseId = courseIds[i % courseIds.Count],
                Title = def.Title,
                Type = def.Type,
                Url = def.Url,
                DurationMinutes = def.DurationMinutes,
                Order = def.Order,
                Description = def.Description,
            });
        }

        db.CourseResources.AddRange(resources);
        await db.SaveChangesAsync();
    }

    private static async Task SeedSkillCatalogue(AppDbContext db)
    {
        if (await db.SkillCategories.AnyAsync())
            return;

        // Universal categories (layer 1 - itenium-wide)
        var generalSoftSkills = new SkillCategoryEntity { Id = 1, Name = "General Soft Skills" };
        var productAnalyseSoftSkills = new SkillCategoryEntity { Id = 2, Name = "Product & Analyse Soft Skills" };
        var generalSkills = new SkillCategoryEntity { Id = 3, Name = "General Skills" };

        // Developer CC-specific categories (layer 2 - team .NET = id 2, Java = id 1)
        var backendDotNet = new SkillCategoryEntity { Id = 4, Name = "Backend .NET", TeamId = 2 };
        var backendJava = new SkillCategoryEntity { Id = 5, Name = "Backend Java", TeamId = 1 };
        var codeGeneral = new SkillCategoryEntity { Id = 6, Name = "Code Skills General", TeamId = 2 };
        var database = new SkillCategoryEntity { Id = 7, Name = "Database", TeamId = 2 };
        var versionControl = new SkillCategoryEntity { Id = 8, Name = "Version Control", TeamId = 2 };
        var codeQuality = new SkillCategoryEntity { Id = 9, Name = "Code Quality", TeamId = 2 };
        var architecture = new SkillCategoryEntity { Id = 10, Name = "Architecture", TeamId = 2 };
        var testingQa = new SkillCategoryEntity { Id = 11, Name = "Testing & QA", TeamId = 2 };
        var devops = new SkillCategoryEntity { Id = 12, Name = "DevOps/Cloud", TeamId = 2 };

        db.SkillCategories.AddRange(
            generalSoftSkills, productAnalyseSoftSkills, generalSkills,
            backendDotNet, backendJava, codeGeneral, database,
            versionControl, codeQuality, architecture, testingQa, devops);

        db.Skills.AddRange(
            // General Soft Skills (universal, checkbox)
            new SkillEntity { Id = 1, Name = "Active Listening", LevelCount = 1, CategoryId = 1 },
            new SkillEntity { Id = 2, Name = "Adaptability", LevelCount = 1, CategoryId = 1 },
            new SkillEntity { Id = 3, Name = "Agile", LevelCount = 1, CategoryId = 1 },
            new SkillEntity { Id = 4, Name = "Business-minded", LevelCount = 1, CategoryId = 1 },
            new SkillEntity { Id = 5, Name = "Collaboration", LevelCount = 1, CategoryId = 1 },
            new SkillEntity { Id = 6, Name = "Communication", LevelCount = 1, CategoryId = 1 },
            new SkillEntity { Id = 7, Name = "Critical Thinking", LevelCount = 1, CategoryId = 1 },
            new SkillEntity { Id = 8, Name = "Innovation", LevelCount = 1, CategoryId = 1 },
            new SkillEntity { Id = 9, Name = "Knowledge Sharing", LevelCount = 1, CategoryId = 1 },
            new SkillEntity { Id = 10, Name = "Mentoring", LevelCount = 1, CategoryId = 1 },
            new SkillEntity { Id = 11, Name = "Positivity", LevelCount = 1, CategoryId = 1 },
            new SkillEntity { Id = 12, Name = "Presenting", LevelCount = 1, CategoryId = 1 },
            new SkillEntity { Id = 13, Name = "Problem-Solving", LevelCount = 1, CategoryId = 1 },
            new SkillEntity { Id = 14, Name = "Project Management", LevelCount = 1, CategoryId = 1 },
            new SkillEntity { Id = 15, Name = "Teamwork", LevelCount = 1, CategoryId = 1 },

            // Product & Analyse Soft Skills (universal, checkbox)
            new SkillEntity { Id = 16, Name = "Decision Making", LevelCount = 1, CategoryId = 2 },
            new SkillEntity { Id = 17, Name = "Interviewing", LevelCount = 1, CategoryId = 2 },
            new SkillEntity { Id = 18, Name = "Negotiation", LevelCount = 1, CategoryId = 2 },
            new SkillEntity { Id = 19, Name = "Prioritization", LevelCount = 1, CategoryId = 2 },
            new SkillEntity { Id = 20, Name = "Stakeholder Management", LevelCount = 1, CategoryId = 2 },

            // General Skills (universal, checkbox)
            new SkillEntity { Id = 21, Name = "Cross Competence", LevelCount = 1, CategoryId = 3 },
            new SkillEntity { Id = 22, Name = "AI and New Tech", LevelCount = 1, CategoryId = 3 },

            // Backend .NET (CC-specific, 7 levels)
            new SkillEntity
            {
                Id = 23,
                Name = "C#",
                LevelCount = 7,
                CategoryId = 4,
                Levels =
                [
                    new SkillLevelEntity { Level = 1, Descriptor = "Basic syntax, OOP" },
                    new SkillLevelEntity { Level = 2, Descriptor = "LINQ, Collections" },
                    new SkillLevelEntity { Level = 3, Descriptor = "Async/await, Generics" },
                    new SkillLevelEntity { Level = 4, Descriptor = "Delegates, Events" },
                    new SkillLevelEntity { Level = 5, Descriptor = "Advanced features (Span<T>, TPL)" },
                    new SkillLevelEntity { Level = 6, Descriptor = "Performance optimization" },
                    new SkillLevelEntity { Level = 7, Descriptor = "Runtime internals, Framework design" },
                ]
            },
            new SkillEntity
            {
                Id = 24,
                Name = ".NET",
                LevelCount = 6,
                CategoryId = 4,
                Levels =
                [
                    new SkillLevelEntity { Level = 1, Descriptor = ".NET 8-10 basics" },
                    new SkillLevelEntity { Level = 2, Descriptor = "Framework libraries" },
                    new SkillLevelEntity { Level = 3, Descriptor = "Configuration management" },
                    new SkillLevelEntity { Level = 4, Descriptor = "Migration scenarios" },
                    new SkillLevelEntity { Level = 5, Descriptor = "Framework extensibility" },
                    new SkillLevelEntity { Level = 6, Descriptor = "Technology roadmap" },
                ]
            },
            new SkillEntity
            {
                Id = 25,
                Name = "Dependency Injection",
                LevelCount = 7,
                CategoryId = 4,
                Levels =
                [
                    new SkillLevelEntity { Level = 1, Descriptor = "DI concept" },
                    new SkillLevelEntity { Level = 2, Descriptor = "DI configuration" },
                    new SkillLevelEntity { Level = 3, Descriptor = "Scopes" },
                    new SkillLevelEntity { Level = 4, Descriptor = "Custom registrations" },
                    new SkillLevelEntity { Level = 5, Descriptor = "DI patterns" },
                    new SkillLevelEntity { Level = 6, Descriptor = "Advanced DI patterns" },
                    new SkillLevelEntity { Level = 7, Descriptor = "DI architecture" },
                ]
            },
            new SkillEntity
            {
                Id = 26,
                Name = "ASP.NET Core",
                LevelCount = 6,
                CategoryId = 4,
                Levels =
                [
                    new SkillLevelEntity { Level = 1, Descriptor = "Controllers, Routing" },
                    new SkillLevelEntity { Level = 2, Descriptor = "Model binding, Validation" },
                    new SkillLevelEntity { Level = 3, Descriptor = "Filters, Middleware" },
                    new SkillLevelEntity { Level = 4, Descriptor = "Custom model binders, Minimal APIs" },
                    new SkillLevelEntity { Level = 5, Descriptor = "Performance tuning" },
                    new SkillLevelEntity { Level = 6, Descriptor = "Framework architecture" },
                ]
            },

            // Backend Java (CC-specific, 7 levels)
            new SkillEntity
            {
                Id = 27,
                Name = "Java & JVM",
                LevelCount = 7,
                CategoryId = 5,
                Levels =
                [
                    new SkillLevelEntity { Level = 1, Descriptor = "Basic syntax, OOP" },
                    new SkillLevelEntity { Level = 2, Descriptor = "Collections, streams, Generics, lambdas" },
                    new SkillLevelEntity { Level = 3, Descriptor = "Dependency management" },
                    new SkillLevelEntity { Level = 4, Descriptor = "Concurrency basics" },
                    new SkillLevelEntity { Level = 5, Descriptor = "Advanced concurrency" },
                    new SkillLevelEntity { Level = 6, Descriptor = "JVM tuning" },
                    new SkillLevelEntity { Level = 7, Descriptor = "JVM internals" },
                ]
            },
            new SkillEntity
            {
                Id = 28,
                Name = "Spring Boot",
                LevelCount = 5,
                CategoryId = 5,
                Levels =
                [
                    new SkillLevelEntity { Level = 1, Descriptor = "Controllers" },
                    new SkillLevelEntity { Level = 2, Descriptor = "Custom starters" },
                    new SkillLevelEntity { Level = 3, Descriptor = "Performance tuning, Security" },
                    new SkillLevelEntity { Level = 4, Descriptor = "Spring internals" },
                    new SkillLevelEntity { Level = 5, Descriptor = "Framework design" },
                ]
            },

            // Code Skills General (CC-specific)
            new SkillEntity
            {
                Id = 29,
                Name = "API Design",
                LevelCount = 7,
                CategoryId = 6,
                Levels =
                [
                    new SkillLevelEntity { Level = 1, Descriptor = "REST API basics" },
                    new SkillLevelEntity { Level = 2, Descriptor = "HTTP status codes" },
                    new SkillLevelEntity { Level = 3, Descriptor = "API versioning" },
                    new SkillLevelEntity { Level = 4, Descriptor = "OpenAPI/Swagger" },
                    new SkillLevelEntity { Level = 5, Descriptor = "API design patterns" },
                    new SkillLevelEntity { Level = 6, Descriptor = "GraphQL/gRPC" },
                    new SkillLevelEntity { Level = 7, Descriptor = "API strategy & governance" },
                ]
            },
            new SkillEntity
            {
                Id = 30,
                Name = "Security",
                LevelCount = 7,
                CategoryId = 6,
                Levels =
                [
                    new SkillLevelEntity { Level = 1, Descriptor = "Basic authentication and authorization" },
                    new SkillLevelEntity { Level = 2, Descriptor = "Authorization policies" },
                    new SkillLevelEntity { Level = 3, Descriptor = "Security best practices" },
                    new SkillLevelEntity { Level = 4, Descriptor = "OAuth2/OpenID Connect" },
                    new SkillLevelEntity { Level = 5, Descriptor = "Security architecture" },
                    new SkillLevelEntity { Level = 6, Descriptor = "Platform security" },
                    new SkillLevelEntity { Level = 7, Descriptor = "Enterprise level security" },
                ]
            },

            // Database (CC-specific)
            new SkillEntity
            {
                Id = 31,
                Name = "DBMS/SQL",
                LevelCount = 6,
                CategoryId = 7,
                Levels =
                [
                    new SkillLevelEntity { Level = 1, Descriptor = "SQL basics (SELECT, WHERE), Joins" },
                    new SkillLevelEntity { Level = 2, Descriptor = "Basic indexing" },
                    new SkillLevelEntity { Level = 3, Descriptor = "Query optimization, ORM, performance" },
                    new SkillLevelEntity { Level = 4, Descriptor = "Complex queries, migrations, DDL" },
                    new SkillLevelEntity { Level = 5, Descriptor = "Advanced EF patterns" },
                    new SkillLevelEntity { Level = 6, Descriptor = "Database architecture strategy" },
                ]
            },

            // Version Control (CC-specific)
            new SkillEntity
            {
                Id = 32,
                Name = "Version Control",
                LevelCount = 6,
                CategoryId = 8,
                Levels =
                [
                    new SkillLevelEntity { Level = 1, Descriptor = "Git clone, commit, push" },
                    new SkillLevelEntity { Level = 2, Descriptor = "Branching, merging, pull requests" },
                    new SkillLevelEntity { Level = 3, Descriptor = "Pull request review, merge strategies, rebase" },
                    new SkillLevelEntity { Level = 4, Descriptor = "Git flow/trunk-based" },
                    new SkillLevelEntity { Level = 5, Descriptor = "Branching strategy" },
                    new SkillLevelEntity { Level = 6, Descriptor = "Git governance" },
                ]
            },

            // Code Quality (CC-specific)
            new SkillEntity
            {
                Id = 33,
                Name = "Clean Code",
                LevelCount = 6,
                CategoryId = 9,
                Levels =
                [
                    new SkillLevelEntity { Level = 1, Descriptor = "Naming, formatting" },
                    new SkillLevelEntity { Level = 2, Descriptor = "Functions, comments" },
                    new SkillLevelEntity { Level = 3, Descriptor = "SOLID principles, DRY, KISS, YAGNI" },
                    new SkillLevelEntity { Level = 4, Descriptor = "Refactoring patterns" },
                    new SkillLevelEntity { Level = 5, Descriptor = "Code smells recognition" },
                    new SkillLevelEntity { Level = 6, Descriptor = "Code quality culture" },
                ]
            },
            new SkillEntity
            {
                Id = 34,
                Name = "Design Patterns",
                LevelCount = 7,
                CategoryId = 9,
                Levels =
                [
                    new SkillLevelEntity { Level = 1, Descriptor = "Understanding design patterns" },
                    new SkillLevelEntity { Level = 2, Descriptor = "Factory, Singleton" },
                    new SkillLevelEntity { Level = 3, Descriptor = "Repository, Strategy" },
                    new SkillLevelEntity { Level = 4, Descriptor = "Dependency Injection patterns" },
                    new SkillLevelEntity { Level = 5, Descriptor = "CQRS, Bounded contexts" },
                    new SkillLevelEntity { Level = 6, Descriptor = "Domain-Driven Design" },
                    new SkillLevelEntity { Level = 7, Descriptor = "Pattern selection strategy" },
                ]
            },

            // Architecture (CC-specific)
            new SkillEntity
            {
                Id = 35,
                Name = "Architecture",
                LevelCount = 6,
                CategoryId = 10,
                Levels =
                [
                    new SkillLevelEntity { Level = 1, Descriptor = "Layered architecture" },
                    new SkillLevelEntity { Level = 2, Descriptor = "Microservices basics" },
                    new SkillLevelEntity { Level = 3, Descriptor = "Clean architecture" },
                    new SkillLevelEntity { Level = 4, Descriptor = "Hexagonal architecture" },
                    new SkillLevelEntity { Level = 5, Descriptor = "Distributed systems" },
                    new SkillLevelEntity { Level = 6, Descriptor = "Enterprise architecture" },
                ]
            },

            // Testing & QA (CC-specific)
            new SkillEntity
            {
                Id = 36,
                Name = "Unit Testing",
                LevelCount = 7,
                CategoryId = 11,
                Levels =
                [
                    new SkillLevelEntity { Level = 1, Descriptor = "xUnit/NUnit basics" },
                    new SkillLevelEntity { Level = 2, Descriptor = "AAA/GWT pattern" },
                    new SkillLevelEntity { Level = 3, Descriptor = "Mocking (Moq/NSubstitute)" },
                    new SkillLevelEntity { Level = 4, Descriptor = "Test coverage 70%+" },
                    new SkillLevelEntity { Level = 5, Descriptor = "TDD practice" },
                    new SkillLevelEntity { Level = 6, Descriptor = "Test architecture" },
                    new SkillLevelEntity { Level = 7, Descriptor = "Testing strategy" },
                ]
            },
            new SkillEntity
            {
                Id = 37,
                Name = "Integration Testing",
                LevelCount = 7,
                CategoryId = 11,
                Levels =
                [
                    new SkillLevelEntity { Level = 1, Descriptor = "Test setup understanding" },
                    new SkillLevelEntity { Level = 2, Descriptor = "API testing (REST)" },
                    new SkillLevelEntity { Level = 3, Descriptor = "Database testing" },
                    new SkillLevelEntity { Level = 4, Descriptor = "Testcontainers" },
                    new SkillLevelEntity { Level = 5, Descriptor = "E2E testing strategy" },
                    new SkillLevelEntity { Level = 6, Descriptor = "Performance testing" },
                    new SkillLevelEntity { Level = 7, Descriptor = "QA architecture" },
                ]
            },

            // DevOps/Cloud (CC-specific)
            new SkillEntity
            {
                Id = 38,
                Name = "CI/CD",
                LevelCount = 7,
                CategoryId = 12,
                Levels =
                [
                    new SkillLevelEntity { Level = 1, Descriptor = "Pipeline understanding" },
                    new SkillLevelEntity { Level = 2, Descriptor = "Monitor pipeline runs" },
                    new SkillLevelEntity { Level = 3, Descriptor = "Read YAML pipelines" },
                    new SkillLevelEntity { Level = 4, Descriptor = "Configure pipelines" },
                    new SkillLevelEntity { Level = 5, Descriptor = "Multi-stage pipelines" },
                    new SkillLevelEntity { Level = 6, Descriptor = "Pipeline optimization" },
                    new SkillLevelEntity { Level = 7, Descriptor = "CI/CD strategy" },
                ]
            }
        );

        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT setval('\"SkillCategories_Id_seq\"', (SELECT MAX(\"Id\") FROM \"SkillCategories\"))");
        await db.Database.ExecuteSqlRawAsync("SELECT setval('\"Skills_Id_seq\"', (SELECT MAX(\"Id\") FROM \"Skills\"))");

        // Skill prerequisites
        db.SkillPrerequisites.AddRange(
            // .NET: ASP.NET Core requires C# and .NET framework knowledge
            new SkillPrerequisiteEntity { SkillId = 26, PrerequisiteSkillId = 23 }, // ASP.NET Core → C#
            new SkillPrerequisiteEntity { SkillId = 26, PrerequisiteSkillId = 24 }, // ASP.NET Core → .NET
                                                                                    // .NET: Dependency Injection requires .NET basics
            new SkillPrerequisiteEntity { SkillId = 25, PrerequisiteSkillId = 24 }  // DI → .NET
        );

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
                await userManager.AddClaimAsync(user, new Claim("team", "1")); // Java team
            }
        }
    }

    private static async Task SeedConsultantProfiles(AppDbContext db)
    {
        if (!await db.ConsultantProfiles.AnyAsync())
        {
            // Assign learner@test.local to the .NET profile (TeamId = 2)
            var learner = db.Users.FirstOrDefault(u => u.Email == "learner@test.local");
            if (learner != null)
            {
                db.ConsultantProfiles.Add(new ConsultantProfileEntity
                {
                    UserId = learner.Id,
                    TeamId = 2, // .NET
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
