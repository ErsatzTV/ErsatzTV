using ErsatzTV.Core.Interfaces.Repositories;
using static ErsatzTV.Application.MediaCards.Mapper;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaCards;

public class GetMusicVideoCardsHandler : IRequestHandler<GetMusicVideoCards, MusicVideoCardResultsViewModel>
{
    private readonly IMusicVideoRepository _musicVideoRepository;

    public GetMusicVideoCardsHandler(IMusicVideoRepository musicVideoRepository) =>
        _musicVideoRepository = musicVideoRepository;

    public async Task<MusicVideoCardResultsViewModel> Handle(
        GetMusicVideoCards request,
        CancellationToken cancellationToken)
    {
        int count = await _musicVideoRepository.GetMusicVideoCount(request.ArtistId);

        List<MusicVideoCardViewModel> results = await _musicVideoRepository
            .GetPagedMusicVideos(request.ArtistId, request.PageNumber, request.PageSize)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new MusicVideoCardResultsViewModel(count, results, None);
    }
}