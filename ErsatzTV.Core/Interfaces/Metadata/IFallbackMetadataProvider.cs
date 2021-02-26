using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface IFallbackMetadataProvider
    {
        ShowMetadata GetFallbackMetadataForShow(string showFolder);
    }
}
