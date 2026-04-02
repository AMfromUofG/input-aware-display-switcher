namespace InputAwareDisplaySwitcher.App.ViewModels;

public abstract class SectionViewModelBase
{
    protected SectionViewModelBase(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public string Title { get; }

    public string Description { get; }
}
