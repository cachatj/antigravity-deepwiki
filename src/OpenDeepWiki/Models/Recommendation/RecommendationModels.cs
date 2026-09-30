namespace OpenDeepWiki.Models.Recommendation;

/// <summary>
/// Recommendation configuration
/// </summary>
public class RecommendationConfig
{
    /// <summary>
    /// Popularity weight (stars, forks, watchers)
    /// </summary>
    public double PopularityWeight { get; set; } = 0.20;

    /// <summary>
    /// Subscription count weight
    /// </summary>
    public double SubscriptionWeight { get; set; } = 0.15;

    /// <summary>
    /// Time decay weight
    /// </summary>
    public double TimeDecayWeight { get; set; } = 0.10;

    /// <summary>
    /// User preference weight (based on historical behavior)
    /// </summary>
    public double UserPreferenceWeight { get; set; } = 0.20;

    /// <summary>
    /// Private repository language match weight
    /// </summary>
    public double PrivateRepoLanguageWeight { get; set; } = 0.20;

    /// <summary>
    /// Collaborative filtering weight
    /// </summary>
    public double CollaborativeWeight { get; set; } = 0.15;
}

/// <summary>
/// Recommendation request
/// </summary>
public class RecommendationRequest
{
    /// <summary>
    /// User ID (optional; anonymous users get popularity-based recommendations)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Number of results to return
    /// </summary>
    public int Limit { get; set; } = 20;

    /// <summary>
    /// Recommendation strategy: default, popular, personalized, explore
    /// </summary>
    public string Strategy { get; set; } = "default";

    /// <summary>
    /// Language filter (optional)
    /// </summary>
    public string? LanguageFilter { get; set; }
}

/// <summary>
/// Recommendation response
/// </summary>
public class RecommendationResponse
{
    /// <summary>
    /// Recommended repository list
    /// </summary>
    public List<RecommendedRepository> Items { get; set; } = new();

    /// <summary>
    /// Recommendation strategy used
    /// </summary>
    public string Strategy { get; set; } = string.Empty;

    /// <summary>
    /// Total candidate count
    /// </summary>
    public int TotalCandidates { get; set; }
}

/// <summary>
/// Recommended repository item
/// </summary>
public class RecommendedRepository
{
    public string Id { get; set; } = string.Empty;
    public string RepoName { get; set; } = string.Empty;
    public string OrgName { get; set; } = string.Empty;
    public string? PrimaryLanguage { get; set; }
    public int StarCount { get; set; }
    public int ForkCount { get; set; }
    public int BookmarkCount { get; set; }
    public int SubscriptionCount { get; set; }
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Combined recommendation score
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Score breakdown
    /// </summary>
    public ScoreBreakdown? ScoreBreakdown { get; set; }

    /// <summary>
    /// Recommendation reason
    /// </summary>
    public string? RecommendReason { get; set; }
}

/// <summary>
/// Score breakdown
/// </summary>
public class ScoreBreakdown
{
    public double Popularity { get; set; }
    public double Subscription { get; set; }
    public double TimeDecay { get; set; }
    public double UserPreference { get; set; }
    public double PrivateRepoLanguage { get; set; }
    public double Collaborative { get; set; }
}

/// <summary>
/// Record user activity request
/// </summary>
public class RecordActivityRequest
{
    public string UserId { get; set; } = string.Empty;
    public string? RepositoryId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public int? Duration { get; set; }
    public string? SearchQuery { get; set; }
    public string? Language { get; set; }
}

/// <summary>
/// Record user activity response
/// </summary>
public class RecordActivityResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Mark not interested request
/// </summary>
public class DislikeRequest
{
    public string UserId { get; set; } = string.Empty;
    public string RepositoryId { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

/// <summary>
/// Mark not interested response
/// </summary>
public class DislikeResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Available languages response
/// </summary>
public class AvailableLanguagesResponse
{
    public List<LanguageInfo> Languages { get; set; } = new();
}

/// <summary>
/// Language info
/// </summary>
public class LanguageInfo
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
