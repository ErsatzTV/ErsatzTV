using System.Text.RegularExpressions;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Interfaces.Trakt;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.MediaCollections;

public class AddTraktListHandler : TraktCommandBase, IRequestHandler<AddTraktList, Either<BaseError, Unit>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IEntityLocker _entityLocker;

    public AddTraktListHandler(
        ITraktApiClient traktApiClient,
        ISearchRepository searchRepository,
        ISearchIndex searchIndex,
        IDbContextFactory<TvContext> dbContextFactory,
        ILogger<AddTraktListHandler> logger,
        IEntityLocker entityLocker)
        : base(traktApiClient, searchRepository, searchIndex, logger)
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
        const string PATTERN = @"(?:https:\/\/trakt\.tv\/users\/)?([\w\-_]+)\/(?:lists\/)?([\w\-_]+)";
        Match match = Regex.Match(request.TraktListUrl, PATTERN);
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
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();

        return await TraktApiClient.GetUserList(parameters.User, parameters.List)
            .BindT(list => SaveList(dbContext, list))
            .BindT(list => SaveListItems(dbContext, list))
            .BindT(list => MatchListItems(dbContext, list))
            .MapT(_ => Unit.Default);

        // match list items (and update in search index)
    }

    private record Parameters(string User, string List);
}
