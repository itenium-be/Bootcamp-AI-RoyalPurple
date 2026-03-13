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
        if (await db.SkillCategories.AnyAsync()) return;

        // ── Skill Categories ────────────────────────────────────────────────
        // Universal (no TeamId) — from Skill_Matrix_Itenium.xlsx Developer sheet
        var cat1 = new SkillCategoryEntity { Id = 1, Name = "General Soft Skills", TeamId = null };
        var cat2 = new SkillCategoryEntity { Id = 2, Name = "Code Skills", TeamId = null };
        var cat3 = new SkillCategoryEntity { Id = 3, Name = "Competence Soft Skills", TeamId = null };
        var cat4 = new SkillCategoryEntity { Id = 4, Name = "Quality & QA", TeamId = null };
        var cat5 = new SkillCategoryEntity { Id = 5, Name = "DevOps", TeamId = null };
        var cat6 = new SkillCategoryEntity { Id = 6, Name = "Specialisaties", TeamId = null };
        var cat7 = new SkillCategoryEntity { Id = 7, Name = "Cross Competence", TeamId = null };
        var cat8 = new SkillCategoryEntity { Id = 8, Name = "Full Stack", TeamId = null };

        // QA (TeamId=4) — from Skill_Matrix_Itenium.xlsx QA sheet
        var cat9 = new SkillCategoryEntity { Id = 9, Name = "General Soft Skills", TeamId = 4 };
        var cat10 = new SkillCategoryEntity { Id = 10, Name = "Soft Skills", TeamId = 4 };
        var cat11 = new SkillCategoryEntity { Id = 11, Name = "Manual Testing", TeamId = 4 };
        var cat12 = new SkillCategoryEntity { Id = 12, Name = "Automated Testing", TeamId = 4 };
        var cat13 = new SkillCategoryEntity { Id = 13, Name = "Specialisaties", TeamId = 4 };
        var cat14 = new SkillCategoryEntity { Id = 14, Name = "Cross Competence", TeamId = 4 };

        // PO & Analysis (TeamId=3) — from Skill_Matrix_Itenium.xlsx BA/FA and PO sheets
        var cat15 = new SkillCategoryEntity { Id = 15, Name = "General Soft Skills", TeamId = 3 };
        var cat16 = new SkillCategoryEntity { Id = 16, Name = "Product & Analyse Soft Skills", TeamId = 3 };
        var cat17 = new SkillCategoryEntity { Id = 17, Name = "Business Analyst", TeamId = 3 };
        var cat18 = new SkillCategoryEntity { Id = 18, Name = "Functional Analyst", TeamId = 3 };
        var cat19 = new SkillCategoryEntity { Id = 19, Name = "Specialisaties", TeamId = 3 };
        var cat20 = new SkillCategoryEntity { Id = 20, Name = "Cross Competence", TeamId = 3 };
        var cat21 = new SkillCategoryEntity { Id = 21, Name = "Product Owner", TeamId = 3 };

        // .NET (TeamId=2) — from Developer_Skill_Experience_Matrix.xlsx
        var cat22 = new SkillCategoryEntity { Id = 22, Name = "Code Skills Backend .NET", TeamId = 2 };

        // Java (TeamId=1) — from Developer_Skill_Experience_Matrix.xlsx
        var cat23 = new SkillCategoryEntity { Id = 23, Name = "Code Skills Backend Java", TeamId = 1 };

        // Universal developer technical categories — from Developer_Skill_Experience_Matrix.xlsx
        var cat24 = new SkillCategoryEntity { Id = 24, Name = "Code Skills General", TeamId = null };
        var cat25 = new SkillCategoryEntity { Id = 25, Name = "Database", TeamId = null };
        var cat26 = new SkillCategoryEntity { Id = 26, Name = "Version Control", TeamId = null };
        var cat27 = new SkillCategoryEntity { Id = 27, Name = "Code Quality", TeamId = null };
        var cat28 = new SkillCategoryEntity { Id = 28, Name = "Architecture", TeamId = null };
        var cat29 = new SkillCategoryEntity { Id = 29, Name = "Testing & QA", TeamId = null };
        var cat30 = new SkillCategoryEntity { Id = 30, Name = "DevOps/Cloud", TeamId = null };
        var cat31 = new SkillCategoryEntity { Id = 31, Name = "Soft Skills", TeamId = null };
        var cat32 = new SkillCategoryEntity { Id = 32, Name = "Frontend", TeamId = null };
        var cat33 = new SkillCategoryEntity { Id = 33, Name = "Security", TeamId = null };
        var cat34 = new SkillCategoryEntity { Id = 34, Name = "AI & New Tech", TeamId = null };
        var cat35 = new SkillCategoryEntity { Id = 35, Name = "Verantwoordelijkheden", TeamId = null };

        db.SkillCategories.AddRange(
            cat1, cat2, cat3, cat4, cat5, cat6, cat7, cat8,
            cat9, cat10, cat11, cat12, cat13, cat14,
            cat15, cat16, cat17, cat18, cat19, cat20, cat21,
            cat22, cat23,
            cat24, cat25, cat26, cat27, cat28, cat29, cat30, cat31, cat32, cat33, cat34, cat35
        );

        // ── Skills ──────────────────────────────────────────────────────────
        // General Soft Skills (universal, cat1)
        var skill1 = new SkillEntity { Id = 1, Name = "Active Listening", LevelCount = 0, CategoryId = 1 };
        var skill2 = new SkillEntity { Id = 2, Name = "Communication", LevelCount = 0, CategoryId = 1 };
        var skill3 = new SkillEntity { Id = 3, Name = "Knowledge Sharing", LevelCount = 0, CategoryId = 1 };
        var skill4 = new SkillEntity { Id = 4, Name = "Presenting", LevelCount = 0, CategoryId = 1 };
        var skill5 = new SkillEntity { Id = 5, Name = "Teamwork", LevelCount = 0, CategoryId = 1 };
        var skill6 = new SkillEntity { Id = 6, Name = "Eagerness to Learn", LevelCount = 0, CategoryId = 1 };
        var skill7 = new SkillEntity { Id = 7, Name = "Logical Thinking", LevelCount = 0, CategoryId = 1 };
        var skill8 = new SkillEntity { Id = 8, Name = "Empathy", LevelCount = 0, CategoryId = 1 };
        var skill9 = new SkillEntity { Id = 9, Name = "Collaboration", LevelCount = 0, CategoryId = 1 };
        var skill10 = new SkillEntity { Id = 10, Name = "Adaptability", LevelCount = 0, CategoryId = 1 };
        var skill11 = new SkillEntity { Id = 11, Name = "Eye for Detail", LevelCount = 0, CategoryId = 1 };
        var skill12 = new SkillEntity { Id = 12, Name = "Critical Thinking", LevelCount = 0, CategoryId = 1 };
        var skill13 = new SkillEntity { Id = 13, Name = "Story Telling", LevelCount = 0, CategoryId = 1 };
        var skill14 = new SkillEntity { Id = 14, Name = "Time Management", LevelCount = 0, CategoryId = 1 };
        var skill15 = new SkillEntity { Id = 15, Name = "Problem-Solving", LevelCount = 0, CategoryId = 1 };
        var skill16 = new SkillEntity { Id = 16, Name = "Business-Minded", LevelCount = 0, CategoryId = 1 };
        var skill17 = new SkillEntity { Id = 17, Name = "Mentoring", LevelCount = 0, CategoryId = 1 };
        var skill18 = new SkillEntity { Id = 18, Name = "Leadership", LevelCount = 0, CategoryId = 1 };
        var skill19 = new SkillEntity { Id = 19, Name = "Coaching", LevelCount = 0, CategoryId = 1 };

        // Code Skills — universal developer (cat2)
        var skill20 = new SkillEntity { Id = 20, Name = "# Coding Language(s)", LevelCount = 0, CategoryId = 2 };
        var skill21 = new SkillEntity { Id = 21, Name = "Version Control", LevelCount = 0, CategoryId = 2 };
        var skill22 = new SkillEntity { Id = 22, Name = "Git", LevelCount = 0, CategoryId = 2 };
        var skill23 = new SkillEntity { Id = 23, Name = "SQL", LevelCount = 0, CategoryId = 2 };
        var skill24 = new SkillEntity { Id = 24, Name = "TDD", LevelCount = 0, CategoryId = 2 };
        var skill25 = new SkillEntity { Id = 25, Name = "Architecture Knowledge", LevelCount = 0, CategoryId = 2 };
        var skill26 = new SkillEntity { Id = 26, Name = "Design Patterns", LevelCount = 0, CategoryId = 2 };
        var skill27 = new SkillEntity { Id = 27, Name = "Technical Communication", LevelCount = 0, CategoryId = 2 };
        var skill28 = new SkillEntity { Id = 28, Name = "Technical Leadership", LevelCount = 0, CategoryId = 2 };
        var skill29 = new SkillEntity { Id = 29, Name = "Code Review", LevelCount = 0, CategoryId = 2 };
        var skill30 = new SkillEntity { Id = 30, Name = "Performance", LevelCount = 0, CategoryId = 2 };

        // Competence Soft Skills — universal developer (cat3)
        var skill31 = new SkillEntity { Id = 31, Name = "Scrum", LevelCount = 0, CategoryId = 3 };
        var skill32 = new SkillEntity { Id = 32, Name = "Agile", LevelCount = 0, CategoryId = 3 };
        var skill33 = new SkillEntity { Id = 33, Name = "Knowledge Sharing", LevelCount = 0, CategoryId = 3 };
        var skill34 = new SkillEntity { Id = 34, Name = "Cross Competence", LevelCount = 0, CategoryId = 3 };
        var skill35 = new SkillEntity { Id = 35, Name = "Mentoring", LevelCount = 0, CategoryId = 3 };

        // Quality & QA — universal developer (cat4)
        var skill36 = new SkillEntity { Id = 36, Name = "# Testing Frameworks", LevelCount = 0, CategoryId = 4 };
        var skill37 = new SkillEntity { Id = 37, Name = "Test Technieken", LevelCount = 0, CategoryId = 4 };
        var skill38 = new SkillEntity { Id = 38, Name = "Unit Testing", LevelCount = 0, CategoryId = 4 };
        var skill39 = new SkillEntity { Id = 39, Name = "Code Quality", LevelCount = 0, CategoryId = 4 };
        var skill40 = new SkillEntity { Id = 40, Name = "Clean Code", LevelCount = 0, CategoryId = 4 };
        var skill41 = new SkillEntity { Id = 41, Name = "TDD", LevelCount = 0, CategoryId = 4 };
        var skill42 = new SkillEntity { Id = 42, Name = "Coding Standards", LevelCount = 0, CategoryId = 4 };

        // DevOps — universal developer (cat5)
        var skill43 = new SkillEntity { Id = 43, Name = "Pipeline Integration", LevelCount = 0, CategoryId = 5 };
        var skill44 = new SkillEntity { Id = 44, Name = "CI/CD", LevelCount = 0, CategoryId = 5 };

        // Specialisaties — universal developer (cat6)
        var skill45 = new SkillEntity { Id = 45, Name = "Stay Relevant", LevelCount = 0, CategoryId = 6 };
        var skill46 = new SkillEntity { Id = 46, Name = "Load Testing", LevelCount = 0, CategoryId = 6 };
        var skill47 = new SkillEntity { Id = 47, Name = "AI and New Tech", LevelCount = 0, CategoryId = 6 };
        var skill48 = new SkillEntity { Id = 48, Name = "Security", LevelCount = 0, CategoryId = 6 };
        var skill49 = new SkillEntity { Id = 49, Name = "UI/UX", LevelCount = 0, CategoryId = 6 };
        var skill50 = new SkillEntity { Id = 50, Name = "Management/Lead", LevelCount = 0, CategoryId = 6 };

        // Cross Competence — universal developer (cat7)
        var skill51 = new SkillEntity { Id = 51, Name = ".NET", LevelCount = 0, CategoryId = 7 };
        var skill52 = new SkillEntity { Id = 52, Name = "Java", LevelCount = 0, CategoryId = 7 };
        var skill53 = new SkillEntity { Id = 53, Name = "Unit Testing", LevelCount = 0, CategoryId = 7 };
        var skill54 = new SkillEntity { Id = 54, Name = "Bug Fixing", LevelCount = 0, CategoryId = 7 };
        var skill55 = new SkillEntity { Id = 55, Name = "Code Complete/Clean Code", LevelCount = 0, CategoryId = 7 };
        var skill56 = new SkillEntity { Id = 56, Name = "Code Conventions", LevelCount = 0, CategoryId = 7 };
        var skill57 = new SkillEntity { Id = 57, Name = "Analyse", LevelCount = 0, CategoryId = 7 };
        var skill58 = new SkillEntity { Id = 58, Name = "Giving Feedback", LevelCount = 0, CategoryId = 7 };
        var skill59 = new SkillEntity { Id = 59, Name = "Give Technical Training", LevelCount = 0, CategoryId = 7 };

        // Full Stack — universal developer (cat8)
        var skill60 = new SkillEntity { Id = 60, Name = "Frontend Framework", LevelCount = 0, CategoryId = 8 };
        var skill61 = new SkillEntity { Id = 61, Name = "CSS/HTML", LevelCount = 0, CategoryId = 8 };
        var skill62 = new SkillEntity { Id = 62, Name = "Unit Testing", LevelCount = 0, CategoryId = 8 };
        var skill63 = new SkillEntity { Id = 63, Name = "Mobile First", LevelCount = 0, CategoryId = 8 };
        var skill64 = new SkillEntity { Id = 64, Name = "E2E", LevelCount = 0, CategoryId = 8 };
        var skill65 = new SkillEntity { Id = 65, Name = "Component Testing", LevelCount = 0, CategoryId = 8 };
        var skill66 = new SkillEntity { Id = 66, Name = "Architecture Knowledge", LevelCount = 0, CategoryId = 8 };
        var skill67 = new SkillEntity { Id = 67, Name = "UI/UX", LevelCount = 0, CategoryId = 8 };
        var skill68 = new SkillEntity { Id = 68, Name = "Security", LevelCount = 0, CategoryId = 8 };
        var skill69 = new SkillEntity { Id = 69, Name = "Performance", LevelCount = 0, CategoryId = 8 };
        var skill70 = new SkillEntity { Id = 70, Name = "TDD", LevelCount = 0, CategoryId = 8 };
        var skill71 = new SkillEntity { Id = 71, Name = "Frontend Testing Frameworks", LevelCount = 0, CategoryId = 8 };

        // QA — General Soft Skills (cat9)
        var skill72 = new SkillEntity { Id = 72, Name = "Adaptability", LevelCount = 0, CategoryId = 9 };
        var skill73 = new SkillEntity { Id = 73, Name = "Active Listening", LevelCount = 0, CategoryId = 9 };
        var skill74 = new SkillEntity { Id = 74, Name = "Critical Thinking", LevelCount = 0, CategoryId = 9 };
        var skill75 = new SkillEntity { Id = 75, Name = "Collaboration", LevelCount = 0, CategoryId = 9 };
        var skill76 = new SkillEntity { Id = 76, Name = "Communication", LevelCount = 0, CategoryId = 9 };
        var skill77 = new SkillEntity { Id = 77, Name = "Agile", LevelCount = 0, CategoryId = 9 };
        var skill78 = new SkillEntity { Id = 78, Name = "Cross Competence", LevelCount = 0, CategoryId = 9 };
        var skill79 = new SkillEntity { Id = 79, Name = "Presenting", LevelCount = 0, CategoryId = 9 };

        // QA — Manual Testing (cat11)
        var skill80 = new SkillEntity { Id = 80, Name = "Test Technieken", LevelCount = 0, CategoryId = 11 };
        var skill81 = new SkillEntity { Id = 81, Name = "# Testing Tools", LevelCount = 0, CategoryId = 11 };
        var skill82 = new SkillEntity { Id = 82, Name = "# Test Management Tool", LevelCount = 0, CategoryId = 11 };
        var skill83 = new SkillEntity { Id = 83, Name = "ISTQB Level Testing", LevelCount = 0, CategoryId = 11 };
        var skill84 = new SkillEntity { Id = 84, Name = "Test Plan en Methodologie", LevelCount = 0, CategoryId = 11 };
        var skill85 = new SkillEntity { Id = 85, Name = "Reporting", LevelCount = 0, CategoryId = 11 };
        var skill86 = new SkillEntity { Id = 86, Name = "Coverage", LevelCount = 0, CategoryId = 11 };
        var skill87 = new SkillEntity { Id = 87, Name = "Project Management", LevelCount = 0, CategoryId = 11 };

        // QA — Automated Testing (cat12)
        var skill88 = new SkillEntity { Id = 88, Name = "# Coding Language(s)", LevelCount = 0, CategoryId = 12 };
        var skill89 = new SkillEntity { Id = 89, Name = "# Automation Tool(s)", LevelCount = 0, CategoryId = 12 };
        var skill90 = new SkillEntity { Id = 90, Name = "Front End", LevelCount = 0, CategoryId = 12 };
        var skill91 = new SkillEntity { Id = 91, Name = "Low Code", LevelCount = 0, CategoryId = 12 };
        var skill92 = new SkillEntity { Id = 92, Name = "ISTQB Level Automation", LevelCount = 0, CategoryId = 12 };
        var skill93 = new SkillEntity { Id = 93, Name = "Data Driven", LevelCount = 0, CategoryId = 12 };
        var skill94 = new SkillEntity { Id = 94, Name = "Continues Integration", LevelCount = 0, CategoryId = 12 };
        var skill95 = new SkillEntity { Id = 95, Name = "Back End", LevelCount = 0, CategoryId = 12 };

        // QA — Specialisaties (cat13)
        var skill96 = new SkillEntity { Id = 96, Name = "UI/UX", LevelCount = 0, CategoryId = 13 };
        var skill97 = new SkillEntity { Id = 97, Name = "Load Testing", LevelCount = 0, CategoryId = 13 };
        var skill98 = new SkillEntity { Id = 98, Name = "Security", LevelCount = 0, CategoryId = 13 };
        var skill99 = new SkillEntity { Id = 99, Name = "AI and New Tech", LevelCount = 0, CategoryId = 13 };
        var skill100 = new SkillEntity { Id = 100, Name = "Stay Relevant", LevelCount = 0, CategoryId = 13 };
        var skill101 = new SkillEntity { Id = 101, Name = "Management/Lead", LevelCount = 0, CategoryId = 13 };

        // QA — Cross Competence (cat14)
        var skill102 = new SkillEntity { Id = 102, Name = "Analyse", LevelCount = 0, CategoryId = 14 };
        var skill103 = new SkillEntity { Id = 103, Name = "Java", LevelCount = 0, CategoryId = 14 };
        var skill104 = new SkillEntity { Id = 104, Name = ".NET", LevelCount = 0, CategoryId = 14 };
        var skill105 = new SkillEntity { Id = 105, Name = "Code Conventions", LevelCount = 0, CategoryId = 14 };
        var skill106 = new SkillEntity { Id = 106, Name = "Code Complete", LevelCount = 0, CategoryId = 14 };
        var skill107 = new SkillEntity { Id = 107, Name = "Giving Feedback", LevelCount = 0, CategoryId = 14 };
        var skill108 = new SkillEntity { Id = 108, Name = "Bug Reporting", LevelCount = 0, CategoryId = 14 };
        var skill109 = new SkillEntity { Id = 109, Name = "Bug Fixing", LevelCount = 0, CategoryId = 14 };
        var skill110 = new SkillEntity { Id = 110, Name = "Unit Testing", LevelCount = 0, CategoryId = 14 };

        // PO & Analysis — General Soft Skills (cat15)
        var skill111 = new SkillEntity { Id = 111, Name = "Active Listening", LevelCount = 0, CategoryId = 15 };
        var skill112 = new SkillEntity { Id = 112, Name = "Communication", LevelCount = 0, CategoryId = 15 };
        var skill113 = new SkillEntity { Id = 113, Name = "Knowledge Sharing", LevelCount = 0, CategoryId = 15 };
        var skill114 = new SkillEntity { Id = 114, Name = "Presenting", LevelCount = 0, CategoryId = 15 };
        var skill115 = new SkillEntity { Id = 115, Name = "Teamwork", LevelCount = 0, CategoryId = 15 };
        var skill116 = new SkillEntity { Id = 116, Name = "Eagerness to Learn", LevelCount = 0, CategoryId = 15 };
        var skill117 = new SkillEntity { Id = 117, Name = "Logical Thinking", LevelCount = 0, CategoryId = 15 };
        var skill118 = new SkillEntity { Id = 118, Name = "Empathy", LevelCount = 0, CategoryId = 15 };
        var skill119 = new SkillEntity { Id = 119, Name = "Collaboration", LevelCount = 0, CategoryId = 15 };
        var skill120 = new SkillEntity { Id = 120, Name = "Adaptability", LevelCount = 0, CategoryId = 15 };
        var skill121 = new SkillEntity { Id = 121, Name = "Eye for Detail", LevelCount = 0, CategoryId = 15 };
        var skill122 = new SkillEntity { Id = 122, Name = "Critical Thinking", LevelCount = 0, CategoryId = 15 };
        var skill123 = new SkillEntity { Id = 123, Name = "Story Telling", LevelCount = 0, CategoryId = 15 };
        var skill124 = new SkillEntity { Id = 124, Name = "Problem-Solving", LevelCount = 0, CategoryId = 15 };
        var skill125 = new SkillEntity { Id = 125, Name = "Business-Minded", LevelCount = 0, CategoryId = 15 };
        var skill126 = new SkillEntity { Id = 126, Name = "Mentoring", LevelCount = 0, CategoryId = 15 };
        var skill127 = new SkillEntity { Id = 127, Name = "Leadership", LevelCount = 0, CategoryId = 15 };
        var skill128 = new SkillEntity { Id = 128, Name = "Coaching", LevelCount = 0, CategoryId = 15 };
        var skill129 = new SkillEntity { Id = 129, Name = "Negotiation", LevelCount = 0, CategoryId = 15 };
        var skill130 = new SkillEntity { Id = 130, Name = "Conflict Resolution", LevelCount = 0, CategoryId = 15 };

        // PO & Analysis — Product & Analyse Soft Skills (cat16)
        var skill131 = new SkillEntity { Id = 131, Name = "Agile", LevelCount = 0, CategoryId = 16 };
        var skill132 = new SkillEntity { Id = 132, Name = "Waterfall", LevelCount = 0, CategoryId = 16 };
        var skill133 = new SkillEntity { Id = 133, Name = "Stakeholder Management", LevelCount = 0, CategoryId = 16 };
        var skill134 = new SkillEntity { Id = 134, Name = "V-Model", LevelCount = 0, CategoryId = 16 };
        var skill135 = new SkillEntity { Id = 135, Name = "Project Management", LevelCount = 0, CategoryId = 16 };
        var skill136 = new SkillEntity { Id = 136, Name = "Scrum", LevelCount = 0, CategoryId = 16 };
        var skill137 = new SkillEntity { Id = 137, Name = "Interviewing", LevelCount = 0, CategoryId = 16 };
        var skill138 = new SkillEntity { Id = 138, Name = "Decision Making", LevelCount = 0, CategoryId = 16 };
        var skill139 = new SkillEntity { Id = 139, Name = "Negotiation", LevelCount = 0, CategoryId = 16 };
        var skill140 = new SkillEntity { Id = 140, Name = "Prioritization", LevelCount = 0, CategoryId = 16 };
        var skill141 = new SkillEntity { Id = 141, Name = "Innovation", LevelCount = 0, CategoryId = 16 };
        var skill142 = new SkillEntity { Id = 142, Name = "Change Management", LevelCount = 0, CategoryId = 16 };

        // Business Analyst (cat17)
        var skill143 = new SkillEntity { Id = 143, Name = "UML", LevelCount = 0, CategoryId = 17 };
        var skill144 = new SkillEntity { Id = 144, Name = "Basic Data Analysis (Excel)", LevelCount = 0, CategoryId = 17 };
        var skill145 = new SkillEntity { Id = 145, Name = "Basic SQL", LevelCount = 0, CategoryId = 17 };
        var skill146 = new SkillEntity { Id = 146, Name = "Collaboration Tools (Jira, Confluence)", LevelCount = 0, CategoryId = 17 };
        var skill147 = new SkillEntity { Id = 147, Name = "Documentation", LevelCount = 0, CategoryId = 17 };
        var skill148 = new SkillEntity { Id = 148, Name = "Business Rule Writing", LevelCount = 0, CategoryId = 17 };
        var skill149 = new SkillEntity { Id = 149, Name = "BPMN", LevelCount = 0, CategoryId = 17 };
        var skill150 = new SkillEntity { Id = 150, Name = "Process Optimisation", LevelCount = 0, CategoryId = 17 };
        var skill151 = new SkillEntity { Id = 151, Name = "Data Modeling", LevelCount = 0, CategoryId = 17 };
        var skill152 = new SkillEntity { Id = 152, Name = "Facilitating Workshops", LevelCount = 0, CategoryId = 17 };
        var skill153 = new SkillEntity { Id = 153, Name = "Writing User Stories", LevelCount = 0, CategoryId = 17 };
        var skill154 = new SkillEntity { Id = 154, Name = "Requirements Gathering", LevelCount = 0, CategoryId = 17 };
        var skill155 = new SkillEntity { Id = 155, Name = "Test Case Writing", LevelCount = 0, CategoryId = 17 };
        var skill156 = new SkillEntity { Id = 156, Name = "Advanced Knowledge of SQL", LevelCount = 0, CategoryId = 17 };
        var skill157 = new SkillEntity { Id = 157, Name = "Knowledge of BI-Tools", LevelCount = 0, CategoryId = 17 };
        var skill158 = new SkillEntity { Id = 158, Name = "IT Architecture", LevelCount = 0, CategoryId = 17 };
        var skill159 = new SkillEntity { Id = 159, Name = "Roadmapping", LevelCount = 0, CategoryId = 17 };
        var skill160 = new SkillEntity { Id = 160, Name = "Enterprise Architecture", LevelCount = 0, CategoryId = 17 };

        // Functional Analyst (cat18)
        var skill161 = new SkillEntity { Id = 161, Name = "UML", LevelCount = 0, CategoryId = 18 };
        var skill162 = new SkillEntity { Id = 162, Name = "Basic Data Analysis (Excel)", LevelCount = 0, CategoryId = 18 };
        var skill163 = new SkillEntity { Id = 163, Name = "Basic SQL", LevelCount = 0, CategoryId = 18 };
        var skill164 = new SkillEntity { Id = 164, Name = "Collaboration Tools (Jira, Confluence)", LevelCount = 0, CategoryId = 18 };
        var skill165 = new SkillEntity { Id = 165, Name = "IT Architecture", LevelCount = 0, CategoryId = 18 };
        var skill166 = new SkillEntity { Id = 166, Name = "BPMN", LevelCount = 0, CategoryId = 18 };
        var skill167 = new SkillEntity { Id = 167, Name = "Technical Documentation", LevelCount = 0, CategoryId = 18 };
        var skill168 = new SkillEntity { Id = 168, Name = "Process Optimisation", LevelCount = 0, CategoryId = 18 };
        var skill169 = new SkillEntity { Id = 169, Name = "Data Modeling", LevelCount = 0, CategoryId = 18 };
        var skill170 = new SkillEntity { Id = 170, Name = "Facilitating Workshops", LevelCount = 0, CategoryId = 18 };
        var skill171 = new SkillEntity { Id = 171, Name = "Writing User Stories", LevelCount = 0, CategoryId = 18 };
        var skill172 = new SkillEntity { Id = 172, Name = "Test Case Writing", LevelCount = 0, CategoryId = 18 };
        var skill173 = new SkillEntity { Id = 173, Name = "System Config", LevelCount = 0, CategoryId = 18 };
        var skill174 = new SkillEntity { Id = 174, Name = "UAT Testing", LevelCount = 0, CategoryId = 18 };
        var skill175 = new SkillEntity { Id = 175, Name = "Advanced Knowledge of SQL", LevelCount = 0, CategoryId = 18 };
        var skill176 = new SkillEntity { Id = 176, Name = "Roadmapping", LevelCount = 0, CategoryId = 18 };
        var skill177 = new SkillEntity { Id = 177, Name = "Release Management", LevelCount = 0, CategoryId = 18 };
        var skill178 = new SkillEntity { Id = 178, Name = "CI/CD", LevelCount = 0, CategoryId = 18 };
        var skill179 = new SkillEntity { Id = 179, Name = "Enterprise Planning", LevelCount = 0, CategoryId = 18 };

        // PO & Analysis — Cross Competence (cat20)
        var skill180 = new SkillEntity { Id = 180, Name = "Manual Testing", LevelCount = 0, CategoryId = 20 };
        var skill181 = new SkillEntity { Id = 181, Name = "Bug Logging", LevelCount = 0, CategoryId = 20 };
        var skill182 = new SkillEntity { Id = 182, Name = "Mock-ups", LevelCount = 0, CategoryId = 20 };
        var skill183 = new SkillEntity { Id = 183, Name = "Knowledge of Integrations/Interfaces", LevelCount = 0, CategoryId = 20 };
        var skill184 = new SkillEntity { Id = 184, Name = "DevOps", LevelCount = 0, CategoryId = 20 };
        var skill185 = new SkillEntity { Id = 185, Name = "Product Backlog", LevelCount = 0, CategoryId = 20 };

        // Product Owner (cat21)
        var skill186 = new SkillEntity { Id = 186, Name = "User Story Writing", LevelCount = 0, CategoryId = 21 };
        var skill187 = new SkillEntity { Id = 187, Name = "Backlog Refinement", LevelCount = 0, CategoryId = 21 };
        var skill188 = new SkillEntity { Id = 188, Name = "Roadmapping", LevelCount = 0, CategoryId = 21 };
        var skill189 = new SkillEntity { Id = 189, Name = "Prioritization", LevelCount = 0, CategoryId = 21 };
        var skill190 = new SkillEntity { Id = 190, Name = "Resource Planning", LevelCount = 0, CategoryId = 21 };
        var skill191 = new SkillEntity { Id = 191, Name = "KPI and Reporting", LevelCount = 0, CategoryId = 21 };
        var skill192 = new SkillEntity { Id = 192, Name = "Risk Management", LevelCount = 0, CategoryId = 21 };
        var skill193 = new SkillEntity { Id = 193, Name = "Release Management", LevelCount = 0, CategoryId = 21 };
        var skill194 = new SkillEntity { Id = 194, Name = "Team Alignment", LevelCount = 0, CategoryId = 21 };
        var skill195 = new SkillEntity { Id = 195, Name = "Product Strategy", LevelCount = 0, CategoryId = 21 };
        var skill196 = new SkillEntity { Id = 196, Name = "Product Vision", LevelCount = 0, CategoryId = 21 };
        var skill197 = new SkillEntity { Id = 197, Name = "Portfolio Management", LevelCount = 0, CategoryId = 21 };
        var skill198 = new SkillEntity { Id = 198, Name = "Product Development Strategy", LevelCount = 0, CategoryId = 21 };

        // .NET Backend — with 7-niveau level descriptors (cat22)
        var skill199 = new SkillEntity
        {
            Id = 199,
            Name = "C#",
            LevelCount = 7,
            CategoryId = 22,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Basis syntax, OOP" },
                new SkillLevelEntity { Level = 2, Descriptor = "LINQ, Collections" },
                new SkillLevelEntity { Level = 3, Descriptor = "Async/await, Generics" },
                new SkillLevelEntity { Level = 4, Descriptor = "Delegates, Events" },
                new SkillLevelEntity { Level = 5, Descriptor = "Advanced features (Span<T>, TPL)" },
                new SkillLevelEntity { Level = 6, Descriptor = "Performance optimization" },
                new SkillLevelEntity { Level = 7, Descriptor = "Runtime internals, Framework design" },
            ]
        };
        var skill200 = new SkillEntity
        {
            Id = 200,
            Name = ".NET",
            LevelCount = 6,
            CategoryId = 22,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = ".NET 8-10 basics" },
                new SkillLevelEntity { Level = 2, Descriptor = "Framework libraries, Logging (Serilog/NLog)" },
                new SkillLevelEntity { Level = 3, Descriptor = "Configuration management" },
                new SkillLevelEntity { Level = 4, Descriptor = "Migration scenarios, Middleware concepts" },
                new SkillLevelEntity { Level = 5, Descriptor = "Framework extensibility, Custom middleware" },
                new SkillLevelEntity { Level = 6, Descriptor = "Technology roadmap" },
            ]
        };
        var skill201 = new SkillEntity
        {
            Id = 201,
            Name = "Dependency Injection",
            LevelCount = 7,
            CategoryId = 22,
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
        };
        var skill202 = new SkillEntity
        {
            Id = 202,
            Name = "ASP.NET Core",
            LevelCount = 6,
            CategoryId = 22,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Controllers, Routing" },
                new SkillLevelEntity { Level = 2, Descriptor = "Model binding, Validation" },
                new SkillLevelEntity { Level = 3, Descriptor = "Filters, Middleware" },
                new SkillLevelEntity { Level = 4, Descriptor = "Custom model binders, Minimal APIs" },
                new SkillLevelEntity { Level = 5, Descriptor = "Performance tuning" },
                new SkillLevelEntity { Level = 6, Descriptor = "Framework architecture" },
            ]
        };

        // Java Backend — with 7-niveau level descriptors (cat23)
        var skill203 = new SkillEntity
        {
            Id = 203,
            Name = "Java & JVM",
            LevelCount = 7,
            CategoryId = 23,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Basis syntax, OOP" },
                new SkillLevelEntity { Level = 2, Descriptor = "Collections, streams, Generics, lambdas" },
                new SkillLevelEntity { Level = 3, Descriptor = "Dependency mgmt" },
                new SkillLevelEntity { Level = 4, Descriptor = "Concurrency basics" },
                new SkillLevelEntity { Level = 5, Descriptor = "Advanced concurrency" },
                new SkillLevelEntity { Level = 6, Descriptor = "JVM tuning" },
                new SkillLevelEntity { Level = 7, Descriptor = "JVM internals" },
            ]
        };
        var skill204 = new SkillEntity
        {
            Id = 204,
            Name = "Java Platform",
            LevelCount = 7,
            CategoryId = 23,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "JDK basics, Maven/Gradle" },
                new SkillLevelEntity { Level = 2, Descriptor = "Routing & validation" },
                new SkillLevelEntity { Level = 3, Descriptor = "Scopes & lifecycle" },
                new SkillLevelEntity { Level = 4, Descriptor = "Multi-module builds" },
                new SkillLevelEntity { Level = 5, Descriptor = "Migration scenarios" },
                new SkillLevelEntity { Level = 6, Descriptor = "Platform standards" },
                new SkillLevelEntity { Level = 7, Descriptor = "JVM roadmap" },
            ]
        };
        var skill205 = new SkillEntity
        {
            Id = 205,
            Name = "Dependency Injection",
            LevelCount = 7,
            CategoryId = 23,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "DI concept" },
                new SkillLevelEntity { Level = 2, Descriptor = "@Component, @Autowired" },
                new SkillLevelEntity { Level = 3, Descriptor = "Filters/interceptors" },
                new SkillLevelEntity { Level = 4, Descriptor = "Conditional beans" },
                new SkillLevelEntity { Level = 5, Descriptor = "Advanced wiring" },
                new SkillLevelEntity { Level = 6, Descriptor = "Modular architecture" },
                new SkillLevelEntity { Level = 7, Descriptor = "DI architecture" },
            ]
        };
        var skill206 = new SkillEntity
        {
            Id = 206,
            Name = "Spring Boot",
            LevelCount = 5,
            CategoryId = 23,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Controllers" },
                new SkillLevelEntity { Level = 2, Descriptor = "Custom starters" },
                new SkillLevelEntity { Level = 3, Descriptor = "Performance tuning, Security" },
                new SkillLevelEntity { Level = 4, Descriptor = "Spring internals" },
                new SkillLevelEntity { Level = 5, Descriptor = "Framework design" },
            ]
        };

        // Code Skills General — universal (cat24)
        var skill207 = new SkillEntity
        {
            Id = 207,
            Name = "API Design",
            LevelCount = 7,
            CategoryId = 24,
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
        };
        var skill208 = new SkillEntity
        {
            Id = 208,
            Name = "Security",
            LevelCount = 7,
            CategoryId = 24,
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
        };
        var skill209 = new SkillEntity
        {
            Id = 209,
            Name = "Performance",
            LevelCount = 6,
            CategoryId = 24,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Awareness" },
                new SkillLevelEntity { Level = 2, Descriptor = "Profiling tools" },
                new SkillLevelEntity { Level = 3, Descriptor = "Caching strategy/Memory management" },
                new SkillLevelEntity { Level = 4, Descriptor = "Load testing" },
                new SkillLevelEntity { Level = 5, Descriptor = "Performance optimization" },
                new SkillLevelEntity { Level = 6, Descriptor = "Scalability patterns" },
            ]
        };

        // Database (cat25)
        var skill210 = new SkillEntity
        {
            Id = 210,
            Name = "DBMS/SQL",
            LevelCount = 7,
            CategoryId = 25,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "SQL basics (SELECT, WHERE), Joins, CRUD operations" },
                new SkillLevelEntity { Level = 2, Descriptor = "Basic indexing, Relationships (1:1, 1:N)" },
                new SkillLevelEntity { Level = 3, Descriptor = "Query optimization, ORM, performance, Eager/lazy loading" },
                new SkillLevelEntity { Level = 4, Descriptor = "Complex queries, migrations, DDL, Stored procedures" },
                new SkillLevelEntity { Level = 5, Descriptor = "Performance tuning, Execution plans" },
                new SkillLevelEntity { Level = 6, Descriptor = "Advanced EF patterns, High-performance systems" },
                new SkillLevelEntity { Level = 7, Descriptor = "Database architecture strategy, Polyglot persistence" },
            ]
        };

        // Version Control (cat26)
        var skill211 = new SkillEntity
        {
            Id = 211,
            Name = "Version Control",
            LevelCount = 7,
            CategoryId = 26,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Git clone, commit, push, Basic conflicts oplossen" },
                new SkillLevelEntity { Level = 2, Descriptor = "Branching, merging, pull request, Branch management" },
                new SkillLevelEntity { Level = 3, Descriptor = "Pull requests review, Merge strategies, rebase, Cherry-pick, stash" },
                new SkillLevelEntity { Level = 4, Descriptor = "Git flow/trunk-based" },
                new SkillLevelEntity { Level = 5, Descriptor = "Monorepo vs polyrepo" },
                new SkillLevelEntity { Level = 6, Descriptor = "Branching strategy, Advanced git workflows" },
                new SkillLevelEntity { Level = 7, Descriptor = "Git governance, SCM strategy" },
            ]
        };

        // Code Quality (cat27)
        var skill212 = new SkillEntity
        {
            Id = 212,
            Name = "Clean Code",
            LevelCount = 7,
            CategoryId = 27,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Naamgeving, formatting, Code comments" },
                new SkillLevelEntity { Level = 2, Descriptor = "Functions, comments, Readable code" },
                new SkillLevelEntity { Level = 3, Descriptor = "SOLID principes, DRY, KISS, YAGNI, Meaningful abstractions" },
                new SkillLevelEntity { Level = 4, Descriptor = "Refactoring patterns, Code review geven" },
                new SkillLevelEntity { Level = 5, Descriptor = "Code smells herkennen, Advanced refactoring" },
                new SkillLevelEntity { Level = 6, Descriptor = "Code quality culture, Architecture review" },
                new SkillLevelEntity { Level = 7, Descriptor = "Quality standards & tooling" },
            ]
        };
        var skill213 = new SkillEntity
        {
            Id = 213,
            Name = "Design Patterns",
            LevelCount = 7,
            CategoryId = 27,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Begrip design patterns, Observer, Decorator" },
                new SkillLevelEntity { Level = 2, Descriptor = "Factory, Singleton, Adapter, Facade" },
                new SkillLevelEntity { Level = 3, Descriptor = "Repository, Strategy, Builder, Prototype" },
                new SkillLevelEntity { Level = 4, Descriptor = "Dependency Injection patterns, Event Sourcing" },
                new SkillLevelEntity { Level = 5, Descriptor = "CQRS, Bounded contexts, Microservices patterns" },
                new SkillLevelEntity { Level = 6, Descriptor = "Domain-Driven Design, Architecture patterns" },
                new SkillLevelEntity { Level = 7, Descriptor = "Pattern selection strategy" },
            ]
        };

        // Testing & QA (cat29)
        var skill214 = new SkillEntity
        {
            Id = 214,
            Name = "Unit Testing",
            LevelCount = 7,
            CategoryId = 29,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "xUnit/NUnit basics, Assertions" },
                new SkillLevelEntity { Level = 2, Descriptor = "AAA/GWT pattern, Parametrized tests" },
                new SkillLevelEntity { Level = 3, Descriptor = "Mocking (Moq/NSubstitute), Test isolation" },
                new SkillLevelEntity { Level = 4, Descriptor = "Test coverage 70%+, Flaky tests vermijden" },
                new SkillLevelEntity { Level = 5, Descriptor = "TDD practice, Advanced mocking" },
                new SkillLevelEntity { Level = 6, Descriptor = "Test architecture, BDD (SpecFlow)" },
                new SkillLevelEntity { Level = 7, Descriptor = "Testing strategy, Quality metrics" },
            ]
        };
        var skill215 = new SkillEntity
        {
            Id = 215,
            Name = "Integration Testing",
            LevelCount = 6,
            CategoryId = 29,
            Levels =
            [
                new SkillLevelEntity { Level = 2, Descriptor = "Test setup begrip" },
                new SkillLevelEntity { Level = 3, Descriptor = "API testing (REST)" },
                new SkillLevelEntity { Level = 4, Descriptor = "Database testing, Test data management" },
                new SkillLevelEntity { Level = 5, Descriptor = "Testcontainers, Performance testing" },
                new SkillLevelEntity { Level = 6, Descriptor = "E2E testing strategy, Load testing (k6/JMeter)" },
                new SkillLevelEntity { Level = 7, Descriptor = "QA architecture, Testing governance" },
            ]
        };
        var skill216 = new SkillEntity
        {
            Id = 216,
            Name = "Code Coverage",
            LevelCount = 7,
            CategoryId = 29,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Tests uitvoeren, Mutation testing begrip" },
                new SkillLevelEntity { Level = 2, Descriptor = "Coverage tool gebruiken, Branch coverage" },
                new SkillLevelEntity { Level = 3, Descriptor = "Coverage 60%+, Critical path focus" },
                new SkillLevelEntity { Level = 4, Descriptor = "Coverage 70%+, Coverage in CI/CD" },
                new SkillLevelEntity { Level = 5, Descriptor = "Coverage strategy, Quality gates" },
                new SkillLevelEntity { Level = 6, Descriptor = "Meaningful coverage, Quality automation" },
                new SkillLevelEntity { Level = 7, Descriptor = "Coverage culture" },
            ]
        };

        // DevOps/Cloud (cat30)
        var skill217 = new SkillEntity
        {
            Id = 217,
            Name = "CI/CD",
            LevelCount = 7,
            CategoryId = 30,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Pipeline begrip, Docker begrip" },
                new SkillLevelEntity { Level = 2, Descriptor = "Pipeline runs monitoren, Build artifacts, Docker images maken" },
                new SkillLevelEntity { Level = 3, Descriptor = "YAML pipelines lezen, Deployment stages, Kubernetes basics" },
                new SkillLevelEntity { Level = 4, Descriptor = "Pipeline configureren, Automated testing in CI, Container orchestration" },
                new SkillLevelEntity { Level = 5, Descriptor = "Multi-stage pipelines, Blue-green, canary, Cloud-native architecture" },
                new SkillLevelEntity { Level = 6, Descriptor = "Pipeline optimization, Infrastructure as Code" },
                new SkillLevelEntity { Level = 7, Descriptor = "CI/CD strategy, DevOps culture & metrics" },
            ]
        };
        var skill218 = new SkillEntity
        {
            Id = 218,
            Name = "Monitoring & Logging",
            LevelCount = 7,
            CategoryId = 30,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Logging gebruiken, Debug tools" },
                new SkillLevelEntity { Level = 2, Descriptor = "Application Insights, Log levels" },
                new SkillLevelEntity { Level = 3, Descriptor = "Structured logging, Performance monitoring" },
                new SkillLevelEntity { Level = 4, Descriptor = "Monitoring dashboards, APM tools" },
                new SkillLevelEntity { Level = 5, Descriptor = "Distributed tracing, Health checks" },
                new SkillLevelEntity { Level = 6, Descriptor = "Alerting strategy, SLA/SLO monitoring" },
                new SkillLevelEntity { Level = 7, Descriptor = "Observability architecture, SRE practices" },
            ]
        };
        var skill219 = new SkillEntity
        {
            Id = 219,
            Name = "Infrastructure/Cloud",
            LevelCount = 5,
            CategoryId = 30,
            Levels =
            [
                new SkillLevelEntity { Level = 3, Descriptor = "Resource management" },
                new SkillLevelEntity { Level = 4, Descriptor = "Cost optimization" },
                new SkillLevelEntity { Level = 5, Descriptor = "IaC (Terraform/Bicep)" },
                new SkillLevelEntity { Level = 6, Descriptor = "Cloud-native design" },
                new SkillLevelEntity { Level = 7, Descriptor = "Enterprise cloud" },
            ]
        };

        // Soft Skills (cat31)
        var skill220 = new SkillEntity
        {
            Id = 220,
            Name = "Communicatie",
            LevelCount = 7,
            CategoryId = 31,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Vragen stellen, Feedback ontvangen" },
                new SkillLevelEntity { Level = 2, Descriptor = "Status updates, Team participatie" },
                new SkillLevelEntity { Level = 3, Descriptor = "Technische uitleg, Documentatie schrijven" },
                new SkillLevelEntity { Level = 4, Descriptor = "Presentaties geven, Cross-team communicatie" },
                new SkillLevelEntity { Level = 5, Descriptor = "Stakeholder management, Conflictoplossing" },
                new SkillLevelEntity { Level = 6, Descriptor = "Complexe communicatie, Negotiation" },
                new SkillLevelEntity { Level = 7, Descriptor = "Strategic communication, Organizational influence" },
            ]
        };
        var skill221 = new SkillEntity
        {
            Id = 221,
            Name = "Samenwerking",
            LevelCount = 7,
            CategoryId = 31,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Code reviews ontvangen, Scrum ceremonies" },
                new SkillLevelEntity { Level = 2, Descriptor = "Pair programming, Knowledge sharing" },
                new SkillLevelEntity { Level = 3, Descriptor = "Code reviews geven, Cross-competence taken" },
                new SkillLevelEntity { Level = 4, Descriptor = "Mentoring juniors, Onboarding support" },
                new SkillLevelEntity { Level = 5, Descriptor = "Technische leiding, Technical decision making" },
                new SkillLevelEntity { Level = 6, Descriptor = "Team coaching, Hiring & interviews" },
                new SkillLevelEntity { Level = 7, Descriptor = "Cross-team leadership, Culture building" },
            ]
        };
        var skill222 = new SkillEntity
        {
            Id = 222,
            Name = "Probleemoplossing",
            LevelCount = 7,
            CategoryId = 31,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Debuggen met hulp, Tickets oppakken" },
                new SkillLevelEntity { Level = 2, Descriptor = "Zelfstandig debuggen, Bugs fixen" },
                new SkillLevelEntity { Level = 3, Descriptor = "Root cause analysis, Features implementeren" },
                new SkillLevelEntity { Level = 4, Descriptor = "Complexe problemen, Technical debt managen" },
                new SkillLevelEntity { Level = 5, Descriptor = "Architectuur problemen, Trade-off decisions" },
                new SkillLevelEntity { Level = 6, Descriptor = "System design, Innovation" },
                new SkillLevelEntity { Level = 7, Descriptor = "Strategic problem solving, Vision & roadmap" },
            ]
        };
        var skill223 = new SkillEntity
        {
            Id = 223,
            Name = "Agile & Scrum",
            LevelCount = 7,
            CategoryId = 31,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Ceremonies bijwonen, User stories begrijpen" },
                new SkillLevelEntity { Level = 2, Descriptor = "Daily stand-ups leiden, Taken inschatten" },
                new SkillLevelEntity { Level = 3, Descriptor = "Backlog refinement, Impediments melden" },
                new SkillLevelEntity { Level = 4, Descriptor = "Sprint planning bijdrage, Process improvement" },
                new SkillLevelEntity { Level = 5, Descriptor = "Estimation & planning, Retrospective faciliteren" },
                new SkillLevelEntity { Level = 6, Descriptor = "Agile coaching, Team maturity" },
                new SkillLevelEntity { Level = 7, Descriptor = "Agile transformation, Organizational agility" },
            ]
        };

        // Frontend (cat32)
        var skill224 = new SkillEntity
        {
            Id = 224,
            Name = "HTML/CSS",
            LevelCount = 7,
            CategoryId = 32,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Semantische HTML, Basis CSS styling" },
                new SkillLevelEntity { Level = 2, Descriptor = "Flexbox, Grid, Mobile-first approach" },
                new SkillLevelEntity { Level = 3, Descriptor = "Responsive design, Browser compatibility" },
                new SkillLevelEntity { Level = 4, Descriptor = "CSS preprocessors (SASS), CSS-in-JS" },
                new SkillLevelEntity { Level = 5, Descriptor = "CSS architecture (BEM), Performance (Critical CSS)" },
                new SkillLevelEntity { Level = 6, Descriptor = "Design systems, CSS optimization" },
                new SkillLevelEntity { Level = 7, Descriptor = "Accessibility standards, Frontend strategy" },
            ]
        };
        var skill225 = new SkillEntity
        {
            Id = 225,
            Name = "JavaScript/TypeScript",
            LevelCount = 7,
            CategoryId = 32,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "ES6 basics, Async/await" },
                new SkillLevelEntity { Level = 2, Descriptor = "DOM manipulation, Fetch API" },
                new SkillLevelEntity { Level = 3, Descriptor = "TypeScript basics, Types, interfaces" },
                new SkillLevelEntity { Level = 4, Descriptor = "Advanced TypeScript, Generics, decorators" },
                new SkillLevelEntity { Level = 5, Descriptor = "Build tools (Webpack/Vite), Module systems" },
                new SkillLevelEntity { Level = 6, Descriptor = "Framework internals, Performance optimization" },
                new SkillLevelEntity { Level = 7, Descriptor = "Technology selection" },
            ]
        };
        var skill226 = new SkillEntity
        {
            Id = 226,
            Name = "Framework",
            LevelCount = 7,
            CategoryId = 32,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "1 framework basics, Props/events, React/Angular/Vue" },
                new SkillLevelEntity { Level = 2, Descriptor = "Component lifecycle, API integratie, Hooks/directives basics" },
                new SkillLevelEntity { Level = 3, Descriptor = "State management, Context/services, Component libraries" },
                new SkillLevelEntity { Level = 4, Descriptor = "Routing, forms, Advanced patterns, Custom hooks/directives" },
                new SkillLevelEntity { Level = 5, Descriptor = "Testing (Jest/Vitest), E2E testing (Playwright), Code splitting" },
                new SkillLevelEntity { Level = 6, Descriptor = "Performance patterns, Architecture patterns, SSR/SSG" },
                new SkillLevelEntity { Level = 7, Descriptor = "Micro-frontends, Strategic frontend vision, Framework strategy" },
            ]
        };
        var skill227 = new SkillEntity
        {
            Id = 227,
            Name = "UI/UX",
            LevelCount = 7,
            CategoryId = 32,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Component gebruik" },
                new SkillLevelEntity { Level = 2, Descriptor = "Responsive layouts" },
                new SkillLevelEntity { Level = 3, Descriptor = "Accessibility basics" },
                new SkillLevelEntity { Level = 4, Descriptor = "UX best practices" },
                new SkillLevelEntity { Level = 5, Descriptor = "Design patterns" },
                new SkillLevelEntity { Level = 6, Descriptor = "User research" },
                new SkillLevelEntity { Level = 7, Descriptor = "UX strategy" },
            ]
        };

        // Verantwoordelijkheden (cat35)
        var skill228 = new SkillEntity
        {
            Id = 228,
            Name = "Project/Scope",
            LevelCount = 7,
            CategoryId = 35,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Taken/tickets" },
                new SkillLevelEntity { Level = 2, Descriptor = "Kleine features" },
                new SkillLevelEntity { Level = 3, Descriptor = "Volledige features" },
                new SkillLevelEntity { Level = 4, Descriptor = "Epics/modules" },
                new SkillLevelEntity { Level = 5, Descriptor = "Subsystemen" },
                new SkillLevelEntity { Level = 6, Descriptor = "Applicatie/platform" },
                new SkillLevelEntity { Level = 7, Descriptor = "Multi-team/organisatie" },
            ]
        };
        var skill229 = new SkillEntity
        {
            Id = 229,
            Name = "Autonomie",
            LevelCount = 7,
            CategoryId = 35,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Begeleiding nodig" },
                new SkillLevelEntity { Level = 2, Descriptor = "Regelmatige check-ins" },
                new SkillLevelEntity { Level = 3, Descriptor = "Zelfstandig met reviews" },
                new SkillLevelEntity { Level = 4, Descriptor = "Volledig zelfstandig" },
                new SkillLevelEntity { Level = 5, Descriptor = "Gidsen van anderen" },
                new SkillLevelEntity { Level = 6, Descriptor = "Technical authority" },
                new SkillLevelEntity { Level = 7, Descriptor = "Strategic leadership" },
            ]
        };
        var skill230 = new SkillEntity
        {
            Id = 230,
            Name = "Impact",
            LevelCount = 7,
            CategoryId = 35,
            Levels =
            [
                new SkillLevelEntity { Level = 1, Descriptor = "Persoonlijke output" },
                new SkillLevelEntity { Level = 2, Descriptor = "Team velocity" },
                new SkillLevelEntity { Level = 3, Descriptor = "Feature delivery" },
                new SkillLevelEntity { Level = 4, Descriptor = "Architecture decisions" },
                new SkillLevelEntity { Level = 5, Descriptor = "Team enabling" },
                new SkillLevelEntity { Level = 6, Descriptor = "Engineering excellence" },
                new SkillLevelEntity { Level = 7, Descriptor = "Business strategy" },
            ]
        };

        db.Skills.AddRange(
            skill1, skill2, skill3, skill4, skill5, skill6, skill7, skill8, skill9, skill10,
            skill11, skill12, skill13, skill14, skill15, skill16, skill17, skill18, skill19,
            skill20, skill21, skill22, skill23, skill24, skill25, skill26, skill27, skill28, skill29, skill30,
            skill31, skill32, skill33, skill34, skill35,
            skill36, skill37, skill38, skill39, skill40, skill41, skill42,
            skill43, skill44,
            skill45, skill46, skill47, skill48, skill49, skill50,
            skill51, skill52, skill53, skill54, skill55, skill56, skill57, skill58, skill59,
            skill60, skill61, skill62, skill63, skill64, skill65, skill66, skill67, skill68, skill69, skill70, skill71,
            skill72, skill73, skill74, skill75, skill76, skill77, skill78, skill79,
            skill80, skill81, skill82, skill83, skill84, skill85, skill86, skill87,
            skill88, skill89, skill90, skill91, skill92, skill93, skill94, skill95,
            skill96, skill97, skill98, skill99, skill100, skill101,
            skill102, skill103, skill104, skill105, skill106, skill107, skill108, skill109, skill110,
            skill111, skill112, skill113, skill114, skill115, skill116, skill117, skill118, skill119, skill120,
            skill121, skill122, skill123, skill124, skill125, skill126, skill127, skill128, skill129, skill130,
            skill131, skill132, skill133, skill134, skill135, skill136, skill137, skill138, skill139, skill140, skill141, skill142,
            skill143, skill144, skill145, skill146, skill147, skill148, skill149, skill150,
            skill151, skill152, skill153, skill154, skill155, skill156, skill157, skill158, skill159, skill160,
            skill161, skill162, skill163, skill164, skill165, skill166, skill167, skill168, skill169, skill170,
            skill171, skill172, skill173, skill174, skill175, skill176, skill177, skill178, skill179,
            skill180, skill181, skill182, skill183, skill184, skill185,
            skill186, skill187, skill188, skill189, skill190, skill191, skill192, skill193, skill194, skill195, skill196, skill197, skill198,
            skill199, skill200, skill201, skill202,
            skill203, skill204, skill205, skill206,
            skill207, skill208, skill209,
            skill210,
            skill211,
            skill212, skill213,
            skill214, skill215, skill216,
            skill217, skill218, skill219,
            skill220, skill221, skill222, skill223,
            skill224, skill225, skill226, skill227,
            skill228, skill229, skill230
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
