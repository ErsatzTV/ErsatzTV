using ErsatzTV.Core.Interfaces.Search;

namespace ErsatzTV.Infrastructure.Search;

public class SearchTargets : ISearchTargets
{
    public event EventHandler OnSearchTargetsChanged;

    public void SearchTargetsChanged()
    {
        OnSearchTargetsChanged?.Invoke(this, EventArgs.Empty);
    }
}
