using System.Diagnostics.CodeAnalysis;
using ErsatzTV.Core;
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
            ScheduleKind = PlayoutScheduleKind.Block,
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
            ],
            ProgramSchedule = new ProgramSchedule(),
            ProgramScheduleAlternates = []
        };

        var referenceData = new PlayoutReferenceData(
            playout.Channel,
            Option<Deco>.None,
            playout.Items,
            playout.Templates.ToList(),
            playout.ProgramSchedule,
            playout.ProgramScheduleAlternates,
            playout.PlayoutHistory.ToList(),
            TimeSpan.Zero);

        Either<BaseError, PlayoutBuildResult> buildResult =
            await blockPlayoutBuilder.Build(
                DateTimeOffset.Now,
                playout,
                referenceData,
                PlayoutBuildMode.Reset,
                cancellationToken);

        return await buildResult.MatchAsync(
            async result =>
            {
                // load playout item details for title
                foreach (PlayoutItem playoutItem in result.AddedItems)
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
                        .Include(mi => (mi as Image).ImageMetadata)
                        .Include(mi => (mi as Image).MediaVersions)
                        .Include(mi => (mi as RemoteStream).RemoteStreamMetadata)
                        .Include(mi => (mi as RemoteStream).MediaVersions)
                        .SelectOneAsync(mi => mi.Id, mi => mi.Id == playoutItem.MediaItemId, cancellationToken);

                    foreach (MediaItem mediaItem in maybeMediaItem)
                    {
                        playoutItem.MediaItem = mediaItem;
                    }
                }

                return result.AddedItems.Map(Mapper.ProjectToViewModel).ToList();
            },
            _ => []);
    }

    private static Block MapToBlock(ReplaceBlockItems request) =>
        new()
        {
            Name = request.Name,
            Minutes = request.Minutes,
            StopScheduling = request.StopScheduling,
            Items = request.Items.Map(MapToBlockItem).ToList()
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
            SearchTitle = request.SearchTitle,
            SearchQuery = request.SearchQuery,
            MediaItemId = request.MediaItemId,
            PlaybackOrder = request.PlaybackOrder
        };
}
