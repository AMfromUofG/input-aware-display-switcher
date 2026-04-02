namespace InputAwareDisplaySwitcher.App.ViewModels;

public sealed class NavigationItemViewModel
{
    public NavigationItemViewModel(string title, string summary, SectionViewModelBase section)
    {
        Title = title;
        Summary = summary;
        Section = section;
    }

    public string Title { get; }

    public string Summary { get; }

    public SectionViewModelBase Section { get; }
}
