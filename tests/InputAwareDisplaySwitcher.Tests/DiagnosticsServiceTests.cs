using InputAwareDisplaySwitcher.Core.Domain.Diagnostics;
using InputAwareDisplaySwitcher.Infrastructure.Diagnostics;

namespace InputAwareDisplaySwitcher.Tests;

public sealed class DiagnosticsServiceTests
{
    [Fact]
    public void Record_AddsStructuredEntryAndRaisesNotification()
    {
        var service = new DiagnosticsService();
        DiagnosticRecord? raisedRecord = null;
        service.RecordAdded += record => raisedRecord = record;

        var record = new DiagnosticRecord
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Category = DiagnosticCategories.Switching,
            EventType = DiagnosticEventTypes.SwitchBlocked,
            Severity = DiagnosticSeverity.Warning,
            Message = "Automatic switching is currently locked.",
            Details = new Dictionary<string, string?>
            {
                ["reason"] = "ManualLockActive"
            }
        };

        service.Record(record);

        Assert.Single(service.Records);
        Assert.Same(record, raisedRecord);
        Assert.Equal("ManualLockActive", service.Records[0].Details["reason"]);
    }
}
