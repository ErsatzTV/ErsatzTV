namespace ErsatzTV.Core.Domain;

public class CollectionEnumeratorState
{
    public int Seed { get; set; }
    public int Index { get; set; }
    public CollectionEnumeratorState Clone() => new CollectionEnumeratorState { Seed = Seed, Index = Index };
}
