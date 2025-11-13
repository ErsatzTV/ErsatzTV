using System.Text.RegularExpressions;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
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
        ISearchRepository searchRepository,
        ISearchIndex searchIndex,
        IFallbackMetadataProvider fallbackMetadataProvider,
        ILanguageCodeService languageCodeService,
        IDbContextFactory<TvContext> dbContextFactory,
        ILogger<AddTraktListHandler> logger,
        IEntityLocker entityLocker)
        : base(traktApiClient, searchRepository, searchIndex, fallbackMetadataProvider, languageCodeService, logger)
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
                p => DoAdd(p, cancellationToken),
                error => Task.FromResult<Either<BaseError, Unit>>(error.Join()));
        }
        finally
        {
            if (request.Unlock)
            {
                _entityLocker.UnlockTrakt();
            }
        }
    }

    private static Validation<BaseError, Parameters> ValidateUrl(AddTraktList request)
    {
        if (!string.IsNullOrWhiteSpace(request.User) && !string.IsNullOrWhiteSpace(request.List))
        {
            return new Parameters(request.User, request.List);
        }

        // if we get a url, ensure it's for trakt.tv
        Match match = Uri.IsWellFormedUriString(request.TraktListUrl, UriKind.Absolute)
            ? MatchTraktListUrl(request.TraktListUrl)
            : ShorthandTraktListRegex().Match(request.TraktListUrl);

        if (match.Success)
        {
            string user = match.Groups[1].Value;
            string list = match.Groups[2].Value;
            return new Parameters(user, list);
        }

        return BaseError.New("Invalid Trakt list url");
    }

    private static Match MatchTraktListUrl(string traktListUrl)
    {
        Match match = UriTraktListRegex().Match(traktListUrl);
        if (!match.Success)
        {
            match = UriTraktListRegex2().Match(traktListUrl);
        }

        return match;
    }

    private async Task<Either<BaseError, Unit>> DoAdd(Parameters parameters, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        Logger.LogDebug("Searching for trakt list: {User}/{List}", parameters.User, parameters.List);
        Either<BaseError, TraktList> maybeList = await TraktApiClient.GetUserList(parameters.User, parameters.List);

        foreach (TraktList list in maybeList.RightToSeq())
        {
            list.User = parameters.User.ToLowerInvariant();
            maybeList = await SaveList(dbContext, list, cancellationToken);
        }

        foreach (TraktList list in maybeList.RightToSeq())
        {
            maybeList = await SaveListItems(dbContext, list);
        }

        foreach (TraktList list in maybeList.RightToSeq())
        {
            // match list items (and update in search index)
            maybeList = await MatchListItems(dbContext, list, cancellationToken);
        }

        return maybeList.Map(_ => Unit.Default);
    }

    [GeneratedRegex(@"https:\/\/trakt\.tv\/users\/([\w\-_]+)\/(?:lists\/)?([\w\-_]+)")]
    private static partial Regex UriTraktListRegex();

    [GeneratedRegex(@"https:\/\/trakt\.tv\/lists\/([\w\-_]+)\/([\w\-_]+)")]
    private static partial Regex UriTraktListRegex2();

    [GeneratedRegex(@"([\w\-_]+)\/(?:lists\/)?([\w\-_]+)")]
    private static partial Regex ShorthandTraktListRegex();

    private sealed record Parameters(string User, string List);
}
