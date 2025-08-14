using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Television.Mapper;

namespace ErsatzTV.Application.Television;

public class GetTelevisionShowByIdHandler : IRequestHandler<GetTelevisionShowById, Option<TelevisionShowViewModel>>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly ISearchRepository _searchRepository;

    public GetTelevisionShowByIdHandler(
        IDbContextFactory<TvContext> dbContextFactory,
        ISearchRepository searchRepository,
        IMediaSourceRepository mediaSourceRepository)
    {
        _dbContextFactory = dbContextFactory;
        _searchRepository = searchRepository;
        _mediaSourceRepository = mediaSourceRepository;
    }

    public async Task<Option<TelevisionShowViewModel>> Handle(
        GetTelevisionShowById request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<Show> maybeShow = await dbContext.Shows
            .AsNoTracking()
            .Include(s => s.LibraryPath)
            .ThenInclude(s => s.Library)
            .Include(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Genres)
            .Include(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Tags)
            .Include(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Studios)
            .Include(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Actors)
            .ThenInclude(a => a.Artwork)
            .Include(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Guids)
            .SelectOneAsync(s => s.Id, s => s.Id == request.Id);

        foreach (Show show in maybeShow)
        {
            Option<JellyfinMediaSource> maybeJellyfin = await _mediaSourceRepository.GetAllJellyfin()
                .Map(list => list.HeadOrNone());

            Option<EmbyMediaSource> maybeEmby = await _mediaSourceRepository.GetAllEmby()
                .Map(list => list.HeadOrNone());

            List<string> mediaCodes = await _searchRepository.GetLanguagesForShow(show);
            List<string> languageCodes = await _searchRepository.GetAllThreeLetterLanguageCodes(mediaCodes);
            return ProjectToViewModel(show, languageCodes, maybeJellyfin, maybeEmby);
        }

        return Option<TelevisionShowViewModel>.None;
    }
}
