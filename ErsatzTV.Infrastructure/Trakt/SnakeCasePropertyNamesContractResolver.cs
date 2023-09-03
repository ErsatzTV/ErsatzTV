using System.Globalization;
using System.Text;
using Newtonsoft.Json.Serialization;

namespace ErsatzTV.Infrastructure.Trakt;

public class DeliminatorSeparatedPropertyNamesContractResolver : DefaultContractResolver
{
    private readonly string _separator;

    protected DeliminatorSeparatedPropertyNamesContractResolver(char separator) =>
        _separator = separator.ToString(CultureInfo.InvariantCulture);

    protected override string ResolvePropertyName(string propertyName)
    {
        var parts = new List<string>();
        var currentWord = new StringBuilder();

        foreach (char c in propertyName)
        {
            if (char.IsUpper(c) && currentWord.Length > 0)
            {
                parts.Add(currentWord.ToString());
                currentWord.Clear();
            }

            currentWord.Append(char.ToLower(c, CultureInfo.InvariantCulture));
        }

        if (currentWord.Length > 0)
        {
            parts.Add(currentWord.ToString());
        }

        return string.Join(_separator, parts.ToArray());
    }
}

public class SnakeCasePropertyNamesContractResolver : DeliminatorSeparatedPropertyNamesContractResolver
{
    public SnakeCasePropertyNamesContractResolver() : base('_')
    {
    }
}
