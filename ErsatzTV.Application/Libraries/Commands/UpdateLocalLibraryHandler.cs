﻿using System.Threading.Channels;
using Dapper;
using ErsatzTV.Application.MediaSources;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Libraries.Mapper;

namespace ErsatzTV.Application.Libraries;

public class UpdateLocalLibraryHandler : LocalLibraryHandlerBase,
    IRequestHandler<UpdateLocalLibrary, Either<BaseError, LocalLibraryViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;
    private readonly IEntityLocker _entityLocker;
    private readonly ChannelWriter<IScannerBackgroundServiceRequest> _scannerWorkerChannel;
    private readonly ISearchIndex _searchIndex;

    public UpdateLocalLibraryHandler(
        ChannelWriter<IScannerBackgroundServiceRequest> scannerWorkerChannel,
        IEntityLocker entityLocker,
        ISearchIndex searchIndex,
        IDbContextFactory<TvContext> dbContextFactory)
    {
        _scannerWorkerChannel = scannerWorkerChannel;
        _entityLocker = entityLocker;
        _searchIndex = searchIndex;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Either<BaseError, LocalLibraryViewModel>> Handle(
        UpdateLocalLibrary request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Parameters> validation = await Validate(dbContext, request);
        return await validation.Apply(parameters => UpdateLocalLibrary(dbContext, parameters));
    }

    private async Task<LocalLibraryViewModel> UpdateLocalLibrary(TvContext dbContext, Parameters parameters)
    {
        (LocalLibrary existing, LocalLibrary incoming) = parameters;
        existing.Name = incoming.Name;

        var toAdd = incoming.Paths
            .Filter(p => existing.Paths.All(ep => NormalizePath(ep.Path) != NormalizePath(p.Path)))
            .ToList();
        var toRemove = existing.Paths
            .Filter(ep => incoming.Paths.All(p => NormalizePath(p.Path) != NormalizePath(ep.Path)))
            .ToList();

        var toRemoveIds = toRemove.Map(lp => lp.Id).ToList();

        await dbContext.Connection.ExecuteAsync(
            "DELETE FROM MediaItem WHERE LibraryPathId IN @Ids",
            new { Ids = toRemoveIds });

        // delete all library folders (children first)
        IOrderedQueryable<LibraryFolder> orderedFolders = dbContext.LibraryFolders
            .Filter(lf => toRemoveIds.Contains(lf.LibraryPathId))
            .OrderByDescending(lp => lp.Path.Length);

        foreach (LibraryFolder folder in orderedFolders)
        {
            await dbContext.Connection.ExecuteAsync(
                "DELETE FROM LibraryFolder WHERE Id = @LibraryFolderId",
                new { LibraryFolderId = folder.Id });
        }

        await dbContext.LibraryPaths
            .Filter(lp => toRemoveIds.Contains(lp.Id))
            .ExecuteDeleteAsync();

        existing.Paths.AddRange(toAdd);

        if (await dbContext.SaveChangesAsync() > 0)
        {
            List<int> itemsToRemove = await dbContext.MediaItems
                .AsNoTracking()
                .Filter(mi => toRemoveIds.Contains(mi.LibraryPathId))
                .Map(mi => mi.Id)
                .ToListAsync();

            await _searchIndex.RemoveItems(itemsToRemove);
            _searchIndex.Commit();
        }

        if ((toAdd.Count > 0 || toRemove.Count > 0) && _entityLocker.LockLibrary(existing.Id))
        {
            await _scannerWorkerChannel.WriteAsync(new ForceScanLocalLibrary(existing.Id));
        }

        return ProjectToViewModel(existing);
    }

    private static Task<Validation<BaseError, Parameters>> Validate(
        TvContext dbContext,
        UpdateLocalLibrary request) =>
        LocalLibraryMustExist(dbContext, request)
            .BindT(parameters => NameMustBeValid(request, parameters.Incoming).MapT(_ => parameters))
            .BindT(
                parameters => PathsMustBeValid(dbContext, parameters.Incoming, parameters.Existing.Id)
                    .MapT(_ => parameters));

    private static Task<Validation<BaseError, Parameters>> LocalLibraryMustExist(
        TvContext dbContext,
        UpdateLocalLibrary request) =>
        dbContext.LocalLibraries
            .Include(ll => ll.Paths)
            .SelectOneAsync(ll => ll.Id, ll => ll.Id == request.Id)
            .MapT(
                existing =>
                {
                    var incoming = new LocalLibrary
                    {
                        Name = request.Name,
                        Paths = request.Paths.Map(p => new LibraryPath { Id = p.Id, Path = p.Path }).ToList(),
                        MediaSourceId = existing.Id
                    };

                    return new Parameters(existing, incoming);
                })
            .Map(o => o.ToValidation<BaseError>("LocalLibrary does not exist."));

    private static string NormalizePath(string path) =>
        Path.GetFullPath(new Uri(path).LocalPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .ToUpperInvariant();

    private sealed record Parameters(LocalLibrary Existing, LocalLibrary Incoming);
}
