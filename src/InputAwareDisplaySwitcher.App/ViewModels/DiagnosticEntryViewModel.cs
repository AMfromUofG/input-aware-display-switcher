using InputAwareDisplaySwitcher.Core.Domain.Diagnostics;

namespace InputAwareDisplaySwitcher.App.ViewModels;

public sealed class DiagnosticEntryViewModel
{
    public DiagnosticEntryViewModel(DiagnosticRecord record)
    {
        TimestampLocal = record.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        Severity = record.Severity.ToString();
        Category = record.Category;
        EventType = record.EventType;
        Message = record.Message;
        Details = record.Details.Count == 0
            ? string.Empty
            : string.Join("; ", record.Details.Select(pair => $"{pair.Key}={pair.Value}"));
    }

    public string TimestampLocal { get; }

    public string Severity { get; }

    public string Category { get; }

    public string EventType { get; }

    public string Message { get; }

    public string Details { get; }
}
