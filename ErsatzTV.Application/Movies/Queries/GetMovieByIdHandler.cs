using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Movies.Mapper;

namespace ErsatzTV.Application.Movies;

public class GetMovieByIdHandler : IRequestHandler<GetMovieById, Option<MovieViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IEmbyPathReplacementService _embyPathReplacementService;
    private readonly IJellyfinPathReplacementService _jellyfinPathReplacementService;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly IPlexPathReplacementService _plexPathReplacementService;

    public GetMovieByIdHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        IMovieRepository movieRepository,
        IMediaSourceRepository mediaSourceRepository,
        IPlexPathReplacementService plexPathReplacementService,
        IJellyfinPathReplacementService jellyfinPathReplacementService,
        IEmbyPathReplacementService embyPathReplacementService)
    {
        _dbContextFactory = dbContextFactory;
        _movieRepository = movieRepository;
        _mediaSourceRepository = mediaSourceRepository;
        _plexPathReplacementService = plexPathReplacementService;
        _jellyfinPathReplacementService = jellyfinPathReplacementService;
        _embyPathReplacementService = embyPathReplacementService;
    }

    public async Task<Option<MovieViewModel>> Handle(
        GetMovieById request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<JellyfinMediaSource> maybeJellyfin = await _mediaSourceRepository.GetAllJellyfin()
            .Map(list => list.HeadOrNone());

        Option<EmbyMediaSource> maybeEmby = await _mediaSourceRepository.GetAllEmby()
            .Map(list => list.HeadOrNone());

        Option<Movie> maybeMovie = await _movieRepository.GetMovie(request.Id);

        Option<MediaVersion> maybeVersion = maybeMovie.Map(m => m.MediaVersions.HeadOrNone()).Flatten();
        var languageCodes = new List<string>();
        foreach (MediaVersion version in maybeVersion)
        {
            var mediaCodes = version.Streams
                .Filter(ms => ms.MediaStreamKind == MediaStreamKind.Audio)
                .Map(ms => ms.Language)
                .ToList();

            languageCodes.AddRange(await dbContext.LanguageCodes.GetAllLanguageCodes(mediaCodes));
        }

        foreach (Movie movie in maybeMovie)
        {
            string localPath = await movie.GetLocalPath(
                _plexPathReplacementService,
                _jellyfinPathReplacementService,
                _embyPathReplacementService,
                false);
            return ProjectToViewModel(movie, localPath, languageCodes, maybeJellyfin, maybeEmby);
        }

        return None;
    }
}
