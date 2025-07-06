using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class CreatePlaylistHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<CreatePlaylist, Either<BaseError, PlaylistViewModel>>
{
    public async Task<Either<BaseError, PlaylistViewModel>> Handle(
        CreatePlaylist request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Playlist> validation = await Validate(dbContext, request);
        return await validation.Apply(profile => PersistPlaylist(dbContext, profile));
    }

    private static async Task<PlaylistViewModel> PersistPlaylist(TvContext dbContext, Playlist playlist)
    {
        await dbContext.Playlists.AddAsync(playlist);
        await dbContext.SaveChangesAsync();
        return Mapper.ProjectToViewModel(playlist);
    }

    private static async Task<Validation<BaseError, Playlist>> Validate(TvContext dbContext, CreatePlaylist request) =>
        await ValidatePlaylistName(dbContext, request).MapT(name => new Playlist
        {
            PlaylistGroupId = request.PlaylistGroupId,
            Name = name
        });

    private static async Task<Validation<BaseError, string>> ValidatePlaylistName(
        TvContext dbContext,
        CreatePlaylist request)
    {
        if (request.Name.Length > 50)
        {
            return BaseError.New($"Playlist name \"{request.Name}\" is invalid");
        }

        Option<Playlist> maybeExisting = await dbContext.Playlists
            .FirstOrDefaultAsync(r => r.PlaylistGroupId == request.PlaylistGroupId && r.Name == request.Name)
            .Map(Optional);

        return maybeExisting.IsSome
            ? BaseError.New($"A playlist named \"{request.Name}\" already exists in that playlist group")
            : Success<BaseError, string>(request.Name);
    }
}
