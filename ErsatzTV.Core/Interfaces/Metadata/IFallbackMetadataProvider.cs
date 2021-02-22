using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface IFallbackMetadataProvider
    {
        TelevisionShowMetadata GetFallbackMetadataForShow(string showFolder);
    }
}
