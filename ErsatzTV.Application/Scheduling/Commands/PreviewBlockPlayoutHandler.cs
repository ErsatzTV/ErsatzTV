using System.Diagnostics.CodeAnalysis;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
public class PreviewBlockPlayoutHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IBlockPlayoutPreviewBuilder blockPlayoutBuilder)
    : IRequestHandler<PreviewBlockPlayout, List<PlayoutItemPreviewViewModel>>
{
    public async Task<List<PlayoutItemPreviewViewModel>> Handle(
        PreviewBlockPlayout request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var template = new Template
        {
            Items = []
        };

        template.Items.Add(
            new TemplateItem
            {
                Block = MapToBlock(request.Data),
                StartTime = TimeSpan.Zero,
                Template = template
            });

        var playout = new Playout
        {
            Channel = new Channel(Guid.NewGuid())
            {
                Number = "1",
                Name = "Block Preview"
            },
            Items = [],
            ProgramSchedulePlayoutType = ProgramSchedulePlayoutType.Block,
            PlayoutHistory = [],
            Templates =
            [
                new PlayoutTemplate
                {
                    DaysOfWeek = PlayoutTemplate.AllDaysOfWeek(),
                    DaysOfMonth = PlayoutTemplate.AllDaysOfMonth(),
                    MonthsOfYear = PlayoutTemplate.AllMonthsOfYear(),
                    Template = template
                }
            ]
        };

        await blockPlayoutBuilder.Build(playout, PlayoutBuildMode.Reset, cancellationToken);

        // load playout item details for title
        foreach (PlayoutItem playoutItem in playout.Items)
        {
            Option<MediaItem> maybeMediaItem = await dbContext.MediaItems
                .AsNoTracking()
                .Include(mi => (mi as Movie).MovieMetadata)
                .Include(mi => (mi as Movie).MediaVersions)
                .Include(mi => (mi as MusicVideo).MusicVideoMetadata)
                .Include(mi => (mi as MusicVideo).MediaVersions)
                .Include(mi => (mi as MusicVideo).Artist)
                .ThenInclude(mm => mm.ArtistMetadata)
                .Include(mi => (mi as Episode).EpisodeMetadata)
                .Include(mi => (mi as Episode).MediaVersions)
                .Include(mi => (mi as Episode).Season)
                .ThenInclude(s => s.SeasonMetadata)
                .Include(mi => (mi as Episode).Season.Show)
                .ThenInclude(s => s.ShowMetadata)
                .Include(mi => (mi as OtherVideo).OtherVideoMetadata)
                .Include(mi => (mi as OtherVideo).MediaVersions)
                .Include(mi => (mi as Song).SongMetadata)
                .Include(mi => (mi as Song).MediaVersions)
                .SelectOneAsync(mi => mi.Id, mi => mi.Id == playoutItem.MediaItemId);

            foreach (MediaItem mediaItem in maybeMediaItem)
            {
                playoutItem.MediaItem = mediaItem;
            }
        }

        return playout.Items.Map(Mapper.ProjectToViewModel).ToList();
    }

    private static Block MapToBlock(ReplaceBlockItems request) =>
        new()
        {
            Minutes = request.Minutes,
            Name = request.Name,
            Items = request.Items.Map(MapToBlockItem).ToList(),
        };

    private static BlockItem MapToBlockItem(int id, ReplaceBlockItem request) =>
        new()
        {
            Id = id,
            Index = request.Index,
            CollectionType = request.CollectionType,
            CollectionId = request.CollectionId,
            MultiCollectionId = request.MultiCollectionId,
            SmartCollectionId = request.SmartCollectionId,
            MediaItemId = request.MediaItemId,
            PlaybackOrder = request.PlaybackOrder
        };
}
