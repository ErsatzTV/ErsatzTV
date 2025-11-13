namespace ErsatzTV.Core.Interfaces.Metadata;

public interface ILanguageCodeCache
{
    IReadOnlyDictionary<string, string[]> CodeToGroupLookup { get; }

    IReadOnlyList<string[]> AllGroups { get; }

    Task Load(CancellationToken cancellationToken);
}
