using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class CreatePlaylistGroupHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<CreatePlaylistGroup, Either<BaseError, PlaylistGroupViewModel>>
{
    public async Task<Either<BaseError, PlaylistGroupViewModel>> Handle(
        CreatePlaylistGroup request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, PlaylistGroup> validation = await Validate(request);
        return await validation.Apply(profile => PersistPlaylistGroup(dbContext, profile));
    }

    private static async Task<PlaylistGroupViewModel> PersistPlaylistGroup(TvContext dbContext, PlaylistGroup playlistGroup)
    {
        await dbContext.PlaylistGroups.AddAsync(playlistGroup);
        await dbContext.SaveChangesAsync();
        return Mapper.ProjectToViewModel(playlistGroup);
    }

    private static Task<Validation<BaseError, PlaylistGroup>> Validate(CreatePlaylistGroup request) =>
        Task.FromResult(ValidateName(request).Map(name => new PlaylistGroup { Name = name, Playlists = [] }));

    private static Validation<BaseError, string> ValidateName(CreatePlaylistGroup createPlaylistGroup) =>
        createPlaylistGroup.NotEmpty(x => x.Name)
            .Bind(_ => createPlaylistGroup.NotLongerThan(50)(x => x.Name));
}
