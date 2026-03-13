using Itenium.SkillForge.Data;
using Itenium.SkillForge.Entities;
using Microsoft.EntityFrameworkCore;

namespace Itenium.SkillForge.WebApi.Tests;

[TestFixture]
public class SkillCatalogueTests : DatabaseTestBase
{
    [Test]
    public async Task SkillCategory_UniversalCategory_HasNoTeam()
    {
        var category = new SkillCategoryEntity { Name = "General Soft Skills" };
        Db.SkillCategories.Add(category);
        await Db.SaveChangesAsync();

        var saved = await Db.SkillCategories.FindAsync(category.Id);
        Assert.That(saved!.TeamId, Is.Null);
    }

    [Test]
    public async Task SkillCategory_CcSpecificCategory_HasTeam()
    {
        var team = new TeamEntity { Name = ".NET" };
        Db.Teams.Add(team);
        await Db.SaveChangesAsync();

        var category = new SkillCategoryEntity { Name = "Backend .NET", TeamId = team.Id };
        Db.SkillCategories.Add(category);
        await Db.SaveChangesAsync();

        var saved = await Db.SkillCategories
            .Include(c => c.Team)
            .FirstAsync(c => c.Id == category.Id);

        Assert.That(saved.TeamId, Is.EqualTo(team.Id));
        Assert.That(saved.Team!.Name, Is.EqualTo(".NET"));
    }

    [Test]
    public async Task Skill_CheckboxSkill_HasLevelCountOne()
    {
        var category = new SkillCategoryEntity { Name = "Soft Skills" };
        Db.SkillCategories.Add(category);
        await Db.SaveChangesAsync();

        var skill = new SkillEntity { Name = "Communication", LevelCount = 1, CategoryId = category.Id };
        Db.Skills.Add(skill);
        await Db.SaveChangesAsync();

        var saved = await Db.Skills.FindAsync(skill.Id);
        Assert.That(saved!.LevelCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Skill_WithLevels_PersistsAllLevelDescriptors()
    {
        var category = new SkillCategoryEntity { Name = "Backend .NET" };
        Db.SkillCategories.Add(category);
        await Db.SaveChangesAsync();

        var skill = new SkillEntity
        {
            Name = "C#",
            LevelCount = 7,
            CategoryId = category.Id,
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
        };
        Db.Skills.Add(skill);
        await Db.SaveChangesAsync();

        var saved = await Db.Skills
            .Include(s => s.Levels)
            .FirstAsync(s => s.Id == skill.Id);

        Assert.That(saved.LevelCount, Is.EqualTo(7));
        Assert.That(saved.Levels, Has.Count.EqualTo(7));
        Assert.That(saved.Levels.OrderBy(l => l.Level).First().Descriptor, Is.EqualTo("Basic syntax, OOP"));
        Assert.That(saved.Levels.OrderBy(l => l.Level).Last().Descriptor, Is.EqualTo("Runtime internals, Framework design"));
    }

    [Test]
    public async Task Skill_WithVariableLevelCount_SupportsOneToSeven()
    {
        var category = new SkillCategoryEntity { Name = "Mixed" };
        Db.SkillCategories.Add(category);
        await Db.SaveChangesAsync();

        var skills = Enumerable.Range(1, 7).Select(n => new SkillEntity
        {
            Name = $"Skill with {n} levels",
            LevelCount = n,
            CategoryId = category.Id,
            Levels = Enumerable.Range(1, n)
                .Select(l => new SkillLevelEntity { Level = l, Descriptor = $"Level {l} descriptor" })
                .ToList()
        }).ToList();

        Db.Skills.AddRange(skills);
        await Db.SaveChangesAsync();

        var savedSkills = await Db.Skills
            .Include(s => s.Levels)
            .Where(s => s.CategoryId == category.Id)
            .ToListAsync();

        Assert.That(savedSkills, Has.Count.EqualTo(7));
        foreach (var saved in savedSkills)
        {
            Assert.That(saved.Levels, Has.Count.EqualTo(saved.LevelCount));
        }
    }

    [Test]
    public async Task Skill_WithPrerequisite_PersistsLink()
    {
        var category = new SkillCategoryEntity { Name = "Backend .NET" };
        Db.SkillCategories.Add(category);
        await Db.SaveChangesAsync();

        var basicSkill = new SkillEntity { Name = "C# Basics", LevelCount = 1, CategoryId = category.Id };
        var advancedSkill = new SkillEntity { Name = "C# Advanced", LevelCount = 7, CategoryId = category.Id };
        Db.Skills.AddRange(basicSkill, advancedSkill);
        await Db.SaveChangesAsync();

        var prerequisite = new SkillPrerequisiteEntity
        {
            SkillId = advancedSkill.Id,
            PrerequisiteSkillId = basicSkill.Id
        };
        Db.SkillPrerequisites.Add(prerequisite);
        await Db.SaveChangesAsync();

        var saved = await Db.Skills
            .Include(s => s.Prerequisites)
            .ThenInclude(p => p.PrerequisiteSkill)
            .FirstAsync(s => s.Id == advancedSkill.Id);

        Assert.That(saved.Prerequisites, Has.Count.EqualTo(1));
        Assert.That(saved.Prerequisites[0].PrerequisiteSkill.Name, Is.EqualTo("C# Basics"));
    }

    [Test]
    public async Task SeedData_UniversalCategories_HaveNoTeam()
    {
        await SeedData.SeedDevelopmentData_ForTest(Db);

        var universalCategories = await Db.SkillCategories
            .Where(c => c.TeamId == null)
            .ToListAsync();

        Assert.That(universalCategories, Is.Not.Empty);
    }

    [Test]
    public async Task SeedData_CcSpecificCategories_HaveTeam()
    {
        await SeedData.SeedDevelopmentData_ForTest(Db);

        var ccCategories = await Db.SkillCategories
            .Where(c => c.TeamId != null)
            .ToListAsync();

        Assert.That(ccCategories, Is.Not.Empty);
    }

    [Test]
    public async Task SeedData_DeveloperSkills_HaveSevenLevelMax()
    {
        await SeedData.SeedDevelopmentData_ForTest(Db);

        var csharpSkill = await Db.Skills
            .Include(s => s.Levels)
            .FirstOrDefaultAsync(s => s.Name == "C#");

        Assert.That(csharpSkill, Is.Not.Null);
        Assert.That(csharpSkill!.LevelCount, Is.EqualTo(7));
        Assert.That(csharpSkill.Levels, Has.Count.EqualTo(7));
    }

    [Test]
    public async Task SeedData_SoftSkills_AreCheckboxes()
    {
        await SeedData.SeedDevelopmentData_ForTest(Db);

        var softSkills = await Db.Skills
            .Include(s => s.Category)
            .Where(s => s.Category.Name == "General Soft Skills")
            .ToListAsync();

        Assert.That(softSkills, Is.Not.Empty);
        Assert.That(softSkills.All(s => s.LevelCount == 1), Is.True);
    }
}
