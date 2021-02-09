using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaSources
{
    public record MediaSourceViewModel(int Id, string Name, MediaSourceType SourceType);
}
