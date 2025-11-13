using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Television.Mapper;

namespace ErsatzTV.Application.Television;

public class GetTelevisionShowByIdHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    ISearchRepository searchRepository,
    ILanguageCodeService languageCodeService,
    IMediaSourceRepository mediaSourceRepository)
    : IRequestHandler<GetTelevisionShowById, Option<TelevisionShowViewModel>>
{
    public async Task<Option<TelevisionShowViewModel>> Handle(
        GetTelevisionShowById request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

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
            .SelectOneAsync(s => s.Id, s => s.Id == request.Id, cancellationToken);

        foreach (Show show in maybeShow)
        {
            Option<JellyfinMediaSource> maybeJellyfin = await mediaSourceRepository.GetAllJellyfin(cancellationToken)
                .Map(list => list.HeadOrNone());

            Option<EmbyMediaSource> maybeEmby = await mediaSourceRepository.GetAllEmby(cancellationToken)
                .Map(list => list.HeadOrNone());

            List<string> mediaCodes = await searchRepository.GetLanguagesForShow(show);
            List<string> languageCodes = languageCodeService.GetAllLanguageCodes(mediaCodes);
            return ProjectToViewModel(show, languageCodes, maybeJellyfin, maybeEmby);
        }

        return Option<TelevisionShowViewModel>.None;
    }
}
