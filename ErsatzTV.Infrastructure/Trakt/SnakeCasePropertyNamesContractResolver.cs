using System.Globalization;
using System.Text;
using Newtonsoft.Json.Serialization;

namespace ErsatzTV.Infrastructure.Trakt;

public class DeliminatorSeparatedPropertyNamesContractResolver : DefaultContractResolver
{
    private readonly string separator;

    protected DeliminatorSeparatedPropertyNamesContractResolver(char separator) =>
        this.separator = separator.ToString(CultureInfo.InvariantCulture);

    protected override string ResolvePropertyName(string propertyName)
    {
        var parts = new List<string>();
        var currentWord = new StringBuilder();

        foreach (char c in propertyName.ToCharArray())
        {
            if (char.IsUpper(c) && currentWord.Length > 0)
            {
                parts.Add(currentWord.ToString());
                currentWord.Clear();
            }

            currentWord.Append(char.ToLower(c));
        }

        if (currentWord.Length > 0)
        {
            parts.Add(currentWord.ToString());
        }

        return string.Join(separator, parts.ToArray());
    }
}

public class SnakeCasePropertyNamesContractResolver : DeliminatorSeparatedPropertyNamesContractResolver
{
    public SnakeCasePropertyNamesContractResolver() : base('_')
    {
    }
}
