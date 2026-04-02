using InputAwareDisplaySwitcher.Core.Domain.Diagnostics;

namespace InputAwareDisplaySwitcher.Core.Application;

public delegate void DiagnosticRecordAddedHandler(DiagnosticRecord record);

public interface IDiagnosticsService
{
    IReadOnlyList<DiagnosticRecord> Records { get; }

    event DiagnosticRecordAddedHandler? RecordAdded;

    void Record(DiagnosticRecord record);

    void Record(
        string category,
        string eventType,
        string message,
        DiagnosticSeverity severity = DiagnosticSeverity.Information,
        IReadOnlyDictionary<string, string?>? details = null);
}
