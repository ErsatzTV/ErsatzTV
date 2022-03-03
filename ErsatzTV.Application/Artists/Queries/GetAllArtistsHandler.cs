using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaItems.Mapper;

namespace ErsatzTV.Application.Artists;

public class GetAllArtistsHandler : IRequestHandler<GetAllArtists, List<NamedMediaItemViewModel>>
{
    private readonly IArtistRepository _artistRepository;

    public GetAllArtistsHandler(IArtistRepository artistRepository) => _artistRepository = artistRepository;

    public Task<List<NamedMediaItemViewModel>> Handle(
        GetAllArtists request,
        CancellationToken cancellationToken) =>
        _artistRepository.GetAllArtists()
            .Map(
                list => list.Filter(
                    a => !string.IsNullOrWhiteSpace(
                        a.ArtistMetadata.HeadOrNone().Match(am => am.Title, () => string.Empty))))
            .Map(list => list.Map(ProjectToViewModel).ToList());
}