using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using MediatR;
using static ErsatzTV.Application.Movies.Mapper;

namespace ErsatzTV.Application.Movies.Queries
{
    public class GetMovieByIdHandler : IRequestHandler<GetMovieById, Option<MovieViewModel>>
    {
        private readonly IMovieRepository _movieRepository;
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public GetMovieByIdHandler(IMovieRepository movieRepository, IMediaSourceRepository mediaSourceRepository)
        {
            _movieRepository = movieRepository;
            _mediaSourceRepository = mediaSourceRepository;
        }

        public async Task<Option<MovieViewModel>> Handle(
            GetMovieById request,
            CancellationToken cancellationToken)
        {
            Option<Movie> movie = await _movieRepository.GetMovie(request.Id);
            Option<MovieViewModel> result = movie.Map(ProjectToViewModel);

            // TODO: generate remote urls for additional artwork
            Option<JellyfinMediaSource> maybeJellyfin = await _mediaSourceRepository.GetAllJellyfin()
                .Map(list => list.HeadOrNone());

            if (maybeJellyfin.IsSome)
            {
                JellyfinMediaSource jellyfin = maybeJellyfin.ValueUnsafe();
                if (result.IsSome)
                {
                    MovieViewModel m = result.ValueUnsafe();

                    var actorsToReplace = m.Actors.Where(vm => (vm.Thumb ?? string.Empty).StartsWith("jellyfin://"))
                        .ToList();

                    foreach (ActorCardViewModel actor in actorsToReplace)
                    {
                        int index = m.Actors.IndexOf(actor);
                        m.Actors.Remove(actor);
                        var newActor = new ActorCardViewModel(
                            actor.Id,
                            actor.Name,
                            actor.Role,
                            actor.Thumb.Replace("jellyfin://", jellyfin.Connections.Head().Address) + "&fillWidth=152");
                        m.Actors.Insert(index, newActor);
                    }

                    if (m.Poster.StartsWith("jellyfin://"))
                    {
                        m.Poster = m.Poster.Replace("jellyfin://", jellyfin.Connections.Head().Address) +
                                   "&fillHeight=440";
                    }

                    if (m.FanArt.StartsWith("jellyfin://"))
                    {
                        m.FanArt = m.FanArt.Replace("jellyfin://", jellyfin.Connections.Head().Address);
                    }
                }
            }

            return result;
        }
    }
}
