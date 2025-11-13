namespace ErsatzTV.Core.Interfaces.Metadata;

public interface ILanguageCodeService
{
    List<string> GetAllLanguageCodes(string mediaCode);

    List<string> GetAllLanguageCodes(List<string> mediaCodes);
}
