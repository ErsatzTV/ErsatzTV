using System.Text.RegularExpressions;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories.Caching;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Interfaces.Trakt;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.MediaCollections;

public partial class AddTraktListHandler : TraktCommandBase, IRequestHandler<AddTraktList, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IEntityLocker _entityLocker;

    public AddTraktListHandler(
        ITraktApiClient traktApiClient,
        ICachingSearchRepository searchRepository,
        ISearchIndex searchIndex,
        IFallbackMetadataProvider fallbackMetadataProvider,
        IDbContextFactory<TvContext> dbContextFactory,
        ILogger<AddTraktListHandler> logger,
        IEntityLocker entityLocker)
        : base(traktApiClient, searchRepository, searchIndex, fallbackMetadataProvider, logger)
    {
        _dbContextFactory = dbContextFactory;
        _entityLocker = entityLocker;
    }

    public async Task<Either<BaseError, Unit>> Handle(AddTraktList request, CancellationToken cancellationToken)
    {
        try
        {
            Validation<BaseError, Parameters> validation = ValidateUrl(request);
            return await validation.Match(
                DoAdd,
                error => Task.FromResult<Either<BaseError, Unit>>(error.Join()));
        }
        finally
        {
            _entityLocker.UnlockTrakt();
        }
    }

    private static Validation<BaseError, Parameters> ValidateUrl(AddTraktList request)
    {
        // if we get a url, ensure it's for trakt.tv
        Match match = Uri.IsWellFormedUriString(request.TraktListUrl, UriKind.Absolute)
            ? UriTraktListRegex().Match(request.TraktListUrl)
            : ShorthandTraktListRegex().Match(request.TraktListUrl);

        if (match.Success)
        {
            string user = match.Groups[1].Value;
            string list = match.Groups[2].Value;
            return new Parameters(user, list);
        }

        return BaseError.New("Invalid Trakt list url");
    }

    private async Task<Either<BaseError, Unit>> DoAdd(Parameters parameters)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        Logger.LogDebug("Searching for trakt list: {User}/{List}", parameters.User, parameters.List);
        Either<BaseError, TraktList> maybeList = await TraktApiClient.GetUserList(parameters.User, parameters.List);

        foreach (TraktList list in maybeList.RightToSeq())
        {
            maybeList = await SaveList(dbContext, list);
        }

        foreach (TraktList list in maybeList.RightToSeq())
        {
            maybeList = await SaveListItems(dbContext, list);
        }

        foreach (TraktList list in maybeList.RightToSeq())
        {
            // match list items (and update in search index)
            maybeList = await MatchListItems(dbContext, list);
        }

        return maybeList.Map(_ => Unit.Default);
    }

    private sealed record Parameters(string User, string List);

    [GeneratedRegex(@"https:\/\/trakt\.tv\/users\/([\w\-_]+)\/(?:lists\/)?([\w\-_]+)")]
    private static partial Regex UriTraktListRegex();

    [GeneratedRegex(@"([\w\-_]+)\/(?:lists\/)?([\w\-_]+)")]
    private static partial Regex ShorthandTraktListRegex();
}
