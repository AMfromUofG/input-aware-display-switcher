namespace InputAwareDisplaySwitcher.App.ViewModels;

public abstract class SectionViewModelBase : ObservableObject, IDisposable
{
    protected SectionViewModelBase(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public string Title { get; }

    public string Description { get; }

    public virtual void Dispose()
    {
    }
}
