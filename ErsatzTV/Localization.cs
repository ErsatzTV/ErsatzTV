namespace ErsatzTV;

public static class Localization
{
    public static readonly List<CultureLanguage> SupportedLanguages =
    [
        new("en-us", "English"),
        new("pl", "Polski"),
        new("pt-br", "Português (Brasil)")
    ];

    public static string DefaultCulture => "en-us";

    public static string[] UiCultures => SupportedLanguages.Map(cl => cl.Culture).ToArray();

    public sealed record CultureLanguage(string Culture, string Language);
}
