using InputAwareDisplaySwitcher.Core.Domain.Switching;

namespace InputAwareDisplaySwitcher.App.ViewModels;

public sealed class PriorityModeOptionViewModel
{
    public PriorityModeOptionViewModel(PriorityMode value, string label, string description)
    {
        Value = value;
        Label = label;
        Description = description;
    }

    public PriorityMode Value { get; }

    public string Label { get; }

    public string Description { get; }
}
