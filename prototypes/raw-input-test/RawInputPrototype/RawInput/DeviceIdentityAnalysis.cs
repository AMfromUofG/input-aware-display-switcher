namespace RawInputPrototype.RawInput;

internal sealed class DeviceIdentityAnalysis
{
    public required string CandidatePersistenceKey { get; init; }

    public required string CandidateFingerprint { get; init; }

    public required string SessionScopedCandidates { get; init; }

    public required string PotentiallyStableCandidates { get; init; }

    public required string ReconciliationMetadata { get; init; }

    public required string RecommendationSummary { get; init; }
}
