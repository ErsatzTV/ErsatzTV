using System.Reflection;
using ErsatzTV.Core.Interfaces.Metadata;

namespace ErsatzTV.Infrastructure.Metadata;

public class LanguageCodeCache : ILanguageCodeCache
{
    public IReadOnlyDictionary<string, string[]> CodeToGroupLookup { get; private set; }

    public IReadOnlyList<string[]> AllGroups { get; private set; }

    public async Task Load(CancellationToken cancellationToken)
    {
        var lookup = new Dictionary<string, string[]>();
        var allGroups = new List<string[]>();

        var assembly = Assembly.GetEntryAssembly();
        if (assembly != null)
        {
            await using Stream resource = assembly.GetManifestResourceStream("ErsatzTV.Resources.ISO-639-2_utf-8.txt");
            if (resource != null)
            {
                using var reader = new StreamReader(resource);
                while (!reader.EndOfStream)
                {
                    string line = await reader.ReadLineAsync(cancellationToken);
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    string[] split = line.Split("|");
                    if (split.Length != 5)
                    {
                        continue;
                    }

                    string[] group = new[] { split[0], split[1] }
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .Distinct()
                        .ToArray();

                    if (group.Length > 0)
                    {
                        allGroups.Add(group);

                        foreach (string code in group)
                        {
                            lookup[code] = group;
                        }
                    }
                }
            }
        }

        CodeToGroupLookup = lookup;
        AllGroups = allGroups;
    }
}
