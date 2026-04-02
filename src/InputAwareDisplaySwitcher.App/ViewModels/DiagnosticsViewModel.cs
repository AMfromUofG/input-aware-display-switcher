using System.Collections.ObjectModel;
using System.Windows;
using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Diagnostics;

namespace InputAwareDisplaySwitcher.App.ViewModels;

public sealed class DiagnosticsViewModel : SectionViewModelBase
{
    private readonly ObservableCollection<DiagnosticEntryViewModel> _records = [];

    public DiagnosticsViewModel(IDiagnosticsService diagnosticsService)
        : base("Diagnostics", "Structured runtime history suitable for later live diagnostics and troubleshooting.")
    {
        Records = new ReadOnlyObservableCollection<DiagnosticEntryViewModel>(_records);

        foreach (var record in diagnosticsService.Records)
        {
            _records.Add(new DiagnosticEntryViewModel(record));
        }

        diagnosticsService.RecordAdded += OnRecordAdded;
    }

    public ReadOnlyObservableCollection<DiagnosticEntryViewModel> Records { get; }

    private void OnRecordAdded(DiagnosticRecord record)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            _records.Add(new DiagnosticEntryViewModel(record));
            return;
        }

        dispatcher.Invoke(() => _records.Add(new DiagnosticEntryViewModel(record)));
    }
}
