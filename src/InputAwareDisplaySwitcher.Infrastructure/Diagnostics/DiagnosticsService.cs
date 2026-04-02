using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Diagnostics;

namespace InputAwareDisplaySwitcher.Infrastructure.Diagnostics;

public sealed class DiagnosticsService : IDiagnosticsService
{
    private readonly List<DiagnosticRecord> _records = [];
    private readonly object _sync = new();
    private readonly int _capacity;

    public DiagnosticsService(int capacity = 500)
    {
        _capacity = capacity <= 0 ? 500 : capacity;
    }

    public event DiagnosticRecordAddedHandler? RecordAdded;

    public IReadOnlyList<DiagnosticRecord> Records
    {
        get
        {
            lock (_sync)
            {
                return _records.ToArray();
            }
        }
    }

    public void Record(DiagnosticRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        lock (_sync)
        {
            if (_records.Count == _capacity)
            {
                _records.RemoveAt(0);
            }

            _records.Add(record);
        }

        RecordAdded?.Invoke(record);
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

        Record(new DiagnosticRecord
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Severity = severity,
            Category = category,
            EventType = eventType,
            Message = message,
            Details = details ?? new Dictionary<string, string?>()
        });
    }
}
