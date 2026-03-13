using Itenium.SkillForge.Entities;

namespace Itenium.SkillForge.WebApi.Controllers;

public record SuggestionRequest(string Title, string? Description, string? Reason);

public record ReviewSuggestionRequest(SuggestionStatus Status, string? ReviewNote);
