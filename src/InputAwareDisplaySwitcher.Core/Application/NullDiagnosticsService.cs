using InputAwareDisplaySwitcher.Core.Domain.Diagnostics;

namespace InputAwareDisplaySwitcher.Core.Application;

public sealed class NullDiagnosticsService : IDiagnosticsService
{
    public static NullDiagnosticsService Instance { get; } = new();

    private NullDiagnosticsService()
    {
    }

    public IReadOnlyList<DiagnosticRecord> Records => Array.Empty<DiagnosticRecord>();

    public event DiagnosticRecordAddedHandler? RecordAdded
    {
        add { }
        remove { }
    }

    public void Record(DiagnosticRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
    }

    public void Record(
        string category,
        string eventType,
        string message,
        DiagnosticSeverity severity = DiagnosticSeverity.Information,
        IReadOnlyDictionary<string, string?>? details = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
    }
}
