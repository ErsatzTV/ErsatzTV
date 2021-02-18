using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Metadata
{
    public class SmartCollectionBuilder : ISmartCollectionBuilder
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public SmartCollectionBuilder(IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public async Task<bool> RefreshSmartCollections(TelevisionEpisodeMediaItem mediaItem)
        {
            var results = new List<bool>();

            foreach (TelevisionMediaCollection collection in GetTelevisionCollections(mediaItem))
            {
                results.Add(await _mediaCollectionRepository.InsertOrIgnore(collection));
            }

            return results.Any(identity);
        }

        private IEnumerable<TelevisionMediaCollection> GetTelevisionCollections(TelevisionEpisodeMediaItem mediaItem)
        {
            var televisionMediaItems = new List<TelevisionEpisodeMediaItem> { mediaItem };

            IEnumerable<TelevisionMediaCollection> televisionShowCollections = televisionMediaItems
                .Map(c => c.Metadata.Title)
                .Distinct().Map(
                    t => new TelevisionMediaCollection
                    {
                        Name = $"{t} - All Seasons",
                        ShowTitle = t,
                        SeasonNumber = null
                    });

            IEnumerable<TelevisionMediaCollection> televisionShowSeasonCollections = televisionMediaItems
                .Map(c => new { c.Metadata.Title, c.Metadata.Season }).Distinct()
                .Map(
                    ts =>
                    {
                        return Optional(ts.Season).Map(
                            sn => new TelevisionMediaCollection
                            {
                                Name = $"{ts.Title} - Season {sn:00}",
                                ShowTitle = ts.Title,
                                SeasonNumber = sn
                            });
                    })
                .Sequence().Flatten();

            return Seq(televisionShowCollections, televisionShowSeasonCollections).Flatten();
        }
    }
}
