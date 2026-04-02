namespace InputAwareDisplaySwitcher.Core.Domain.Diagnostics;

public sealed record DiagnosticRecord
{
    public required DateTimeOffset TimestampUtc { get; init; }

    public required DiagnosticSeverity Severity { get; init; }

    public required string Category { get; init; }

    public required string EventType { get; init; }

    public required string Message { get; init; }

    public IReadOnlyDictionary<string, string?> Details { get; init; } = new Dictionary<string, string?>();
}
