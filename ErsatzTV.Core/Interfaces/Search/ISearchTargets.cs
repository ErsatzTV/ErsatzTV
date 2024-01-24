namespace ErsatzTV.Core.Interfaces.Search;

public interface ISearchTargets
{
    event EventHandler OnSearchTargetsChanged;

    void SearchTargetsChanged();
}
