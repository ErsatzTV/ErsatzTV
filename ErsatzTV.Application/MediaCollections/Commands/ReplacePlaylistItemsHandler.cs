using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class ReplacePlaylistItemsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<ReplacePlaylistItems, Either<BaseError, List<PlaylistItemViewModel>>>
{
    public async Task<Either<BaseError, List<PlaylistItemViewModel>>> Handle(
        ReplacePlaylistItems request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Playlist> validation = await Validate(dbContext, request);
        return await validation.Apply(ps => Persist(dbContext, request, ps));
    }

    private static async Task<List<PlaylistItemViewModel>> Persist(
        TvContext dbContext,
        ReplacePlaylistItems request,
        Playlist playlist)
    {
        playlist.Name = request.Name;
        //playlist.DateUpdated = DateTime.UtcNow;

        dbContext.RemoveRange(playlist.Items);
        playlist.Items = request.Items.Map(i => BuildItem(playlist, i.Index, i)).ToList();

        await dbContext.SaveChangesAsync();

        return playlist.Items.Map(Mapper.ProjectToViewModel).ToList();
    }

    private static PlaylistItem BuildItem(Playlist playlist, int index, ReplacePlaylistItem item) =>
        new()
        {
            PlaylistId = playlist.Id,
            Index = index,
            CollectionType = item.CollectionType,
            CollectionId = item.CollectionId,
            MultiCollectionId = item.MultiCollectionId,
            SmartCollectionId = item.SmartCollectionId,
            MediaItemId = item.MediaItemId,
            PlaybackOrder = item.PlaybackOrder,
            IncludeInProgramGuide = item.IncludeInProgramGuide
        };

    private static Task<Validation<BaseError, Playlist>> Validate(TvContext dbContext, ReplacePlaylistItems request) =>
        PlaylistMustExist(dbContext, request.PlaylistId)
            .BindT(playlist => CollectionTypesMustBeValid(request, playlist));

    private static Task<Validation<BaseError, Playlist>> PlaylistMustExist(TvContext dbContext, int playlistId) =>
        dbContext.Playlists
            .Include(b => b.Items)
            .SelectOneAsync(b => b.Id, b => b.Id == playlistId)
            .Map(o => o.ToValidation<BaseError>("[PlaylistId] does not exist."));

    private static Validation<BaseError, Playlist> CollectionTypesMustBeValid(ReplacePlaylistItems request, Playlist playlist) =>
        request.Items.Map(item => CollectionTypeMustBeValid(item, playlist)).Sequence().Map(_ => playlist);

    private static Validation<BaseError, Playlist> CollectionTypeMustBeValid(ReplacePlaylistItem item, Playlist playlist)
    {
        switch (item.CollectionType)
        {
            case ProgramScheduleItemCollectionType.Collection:
                if (item.CollectionId is null)
                {
                    return BaseError.New("[Collection] is required for collection type 'Collection'");
                }

                break;
            case ProgramScheduleItemCollectionType.TelevisionShow:
                if (item.MediaItemId is null)
                {
                    return BaseError.New("[MediaItem] is required for collection type 'TelevisionShow'");
                }

                break;
            case ProgramScheduleItemCollectionType.TelevisionSeason:
                if (item.MediaItemId is null)
                {
                    return BaseError.New("[MediaItem] is required for collection type 'TelevisionSeason'");
                }

                break;
            case ProgramScheduleItemCollectionType.Artist:
                if (item.MediaItemId is null)
                {
                    return BaseError.New("[MediaItem] is required for collection type 'Artist'");
                }

                break;
            case ProgramScheduleItemCollectionType.MultiCollection:
                if (item.MultiCollectionId is null)
                {
                    return BaseError.New("[MultiCollection] is required for collection type 'MultiCollection'");
                }

                break;
            case ProgramScheduleItemCollectionType.SmartCollection:
                if (item.SmartCollectionId is null)
                {
                    return BaseError.New("[SmartCollection] is required for collection type 'SmartCollection'");
                }

                break;
            case ProgramScheduleItemCollectionType.Movie:
            case ProgramScheduleItemCollectionType.Episode:
            case ProgramScheduleItemCollectionType.MusicVideo:
            case ProgramScheduleItemCollectionType.OtherVideo:
            case ProgramScheduleItemCollectionType.Song:
            case ProgramScheduleItemCollectionType.Image:
                if (item.MediaItemId is null)
                {
                    return BaseError.New($"[MediaItem] is required for type '{item.CollectionType}'");
                }

                break;
            case ProgramScheduleItemCollectionType.FakeCollection:
            default:
                return BaseError.New("[CollectionType] is invalid");
        }

        return playlist;
    }
}
