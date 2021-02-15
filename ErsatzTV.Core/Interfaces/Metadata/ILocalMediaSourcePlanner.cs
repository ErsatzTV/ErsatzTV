using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ILocalMediaSourcePlanner
    {
        public Seq<LocalMediaSourcePlan> DetermineActions(
            MediaType mediaType,
            Seq<MediaItem> mediaItems,
            Seq<string> files);
    }
}
