namespace ErsatzTV.Core.Metadata;

public static class SortTitle
{
    public static string GetSortTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return title;
        }

        if (title.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
        {
            return title[4..];
        }

        if (title.StartsWith("a ", StringComparison.OrdinalIgnoreCase))
        {
            return title[2..];
        }

        if (title.StartsWith("an ", StringComparison.OrdinalIgnoreCase))
        {
            return title[3..];
        }

        if (title.StartsWith("Æ"))
        {
            return title.Replace("Æ", "E");
        }

        return title;
    }
}
