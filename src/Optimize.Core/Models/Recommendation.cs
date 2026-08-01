namespace Optimize.Core.Models;

public enum RecommendationSeverity
{
    Information,
    Warning,
    Critical
}

public sealed record Recommendation(
    string Title,
    string Description,
    RecommendationSeverity Severity,
    int ScoreImpact,
    bool RequiresAdministrator = false);
