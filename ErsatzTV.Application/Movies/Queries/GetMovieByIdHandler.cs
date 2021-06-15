using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Movies.Mapper;

namespace ErsatzTV.Application.Movies.Queries
{
    public class GetMovieByIdHandler : IRequestHandler<GetMovieById, Option<MovieViewModel>>
    {
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;
        private readonly IMovieRepository _movieRepository;

        public GetMovieByIdHandler(
            IDbContextFactory<TvContext> dbContextFactory,
            IMovieRepository movieRepository,
            IMediaSourceRepository mediaSourceRepository)
        {
            _dbContextFactory = dbContextFactory;
            _movieRepository = movieRepository;
            _mediaSourceRepository = mediaSourceRepository;
        }

        public async Task<Option<MovieViewModel>> Handle(
            GetMovieById request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            
            Option<JellyfinMediaSource> maybeJellyfin = await _mediaSourceRepository.GetAllJellyfin()
                .Map(list => list.HeadOrNone());

            Option<EmbyMediaSource> maybeEmby = await _mediaSourceRepository.GetAllEmby()
                .Map(list => list.HeadOrNone());

            Option<Movie> movie = await _movieRepository.GetMovie(request.Id);

            Option<MediaVersion> maybeVersion = movie.Map(m => m.MediaVersions.HeadOrNone()).Flatten();
            var languageCodes = new List<string>();
            foreach (MediaVersion version in maybeVersion)
            {
                var mediaCodes = version.Streams
                    .Filter(ms => ms.MediaStreamKind == MediaStreamKind.Audio)
                    .Map(ms => ms.Language)
                    .ToList();

                languageCodes.AddRange(await dbContext.LanguageCodes.GetAllLanguageCodes(mediaCodes));
            }

            return movie.Map(m => ProjectToViewModel(m, languageCodes, maybeJellyfin, maybeEmby));
        }
    }
}
