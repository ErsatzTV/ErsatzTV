using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application.MediaSources.Commands;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Libraries.Mapper;

namespace ErsatzTV.Application.Libraries.Commands
{
    public class UpdateLocalLibraryHandler : LocalLibraryHandlerBase,
        IRequestHandler<UpdateLocalLibrary, Either<BaseError, LocalLibraryViewModel>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _workerChannel;
        private readonly IEntityLocker _entityLocker;
        private readonly ISearchIndex _searchIndex;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public UpdateLocalLibraryHandler(
            ChannelWriter<IBackgroundServiceRequest> workerChannel,
            IEntityLocker entityLocker,
            ISearchIndex searchIndex,
            IDbContextFactory<TvContext> dbContextFactory)
        {
            _workerChannel = workerChannel;
            _entityLocker = entityLocker;
            _searchIndex = searchIndex;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<Either<BaseError, LocalLibraryViewModel>> Handle(
            UpdateLocalLibrary request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            Validation<BaseError, Parameters> validation = await Validate(dbContext, request);
            return await validation.Apply(parameters => UpdateLocalLibrary(dbContext, parameters));
        }

        private async Task<LocalLibraryViewModel> UpdateLocalLibrary(TvContext dbContext, Parameters parameters)
        {
            (LocalLibrary existing, LocalLibrary incoming) = parameters;
            existing.Name = incoming.Name;
            
            // toAdd
            var toAdd = incoming.Paths
                .Filter(p => existing.Paths.All(ep => NormalizePath(ep.Path) != NormalizePath(p.Path)))
                .ToList();
            var toRemove = existing.Paths
                .Filter(ep => incoming.Paths.All(p => NormalizePath(p.Path) != NormalizePath(ep.Path)))
                .ToList();

            var toRemoveIds = toRemove.Map(lp => lp.Id).ToList();

            List<int> itemsToRemove = await dbContext.MediaItems
                .Filter(mi => toRemoveIds.Contains(mi.LibraryPathId))
                .Map(mi => mi.Id)
                .ToListAsync();
            
            existing.Paths.RemoveAll(toRemove.Contains);
            existing.Paths.AddRange(toAdd);

            if (await dbContext.SaveChangesAsync() > 0)
            {
                await _searchIndex.RemoveItems(itemsToRemove);
                _searchIndex.Commit();
            }

            if (toAdd.Count > 0 || toRemove.Count > 0 && _entityLocker.LockLibrary(existing.Id))
            {
                await _workerChannel.WriteAsync(new ForceScanLocalLibrary(existing.Id));
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

        private record Parameters(LocalLibrary Existing, LocalLibrary Incoming);
        
        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .ToUpperInvariant();
        }
    }
}
