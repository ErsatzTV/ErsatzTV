using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Plex.Commands
{
    public class
        UpdatePlexPathReplacementsHandler : MediatR.IRequestHandler<UpdatePlexPathReplacements, Either<BaseError, Unit>>
    {
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public UpdatePlexPathReplacementsHandler(IMediaSourceRepository mediaSourceRepository) =>
            _mediaSourceRepository = mediaSourceRepository;

        public Task<Either<BaseError, Unit>> Handle(
            UpdatePlexPathReplacements request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(pms => MergePathReplacements(request, pms))
                .Bind(v => v.ToEitherAsync());

        private Task<Unit> MergePathReplacements(UpdatePlexPathReplacements request, PlexMediaSource plexMediaSource)
        {
            plexMediaSource.PathReplacements ??= new List<PlexPathReplacement>();

            var incoming = request.PathReplacements.Map(Project).ToList();

            var toAdd = incoming.Filter(r => r.Id < 1).ToList();
            var toRemove = plexMediaSource.PathReplacements.Filter(r => incoming.All(pr => pr.Id != r.Id)).ToList();
            var toUpdate = incoming.Except(toAdd).ToList();

            plexMediaSource.PathReplacements.AddRange(toAdd);
            toRemove.ForEach(pr => plexMediaSource.PathReplacements.Remove(pr));
            foreach (PlexPathReplacement pathReplacement in toUpdate)
            {
                Optional(plexMediaSource.PathReplacements.SingleOrDefault(pr => pr.Id == pathReplacement.Id))
                    .IfSome(
                        pr =>
                        {
                            pr.PlexPath = pathReplacement.PlexPath;
                            pr.LocalPath = pathReplacement.LocalPath;
                        });
            }

            return _mediaSourceRepository.Update(plexMediaSource).ToUnit();
        }

        private static PlexPathReplacement Project(PlexPathReplacementItem vm) =>
            new() { Id = vm.Id, PlexPath = vm.PlexPath, LocalPath = vm.LocalPath };

        private Task<Validation<BaseError, PlexMediaSource>> Validate(UpdatePlexPathReplacements request) =>
            PlexMediaSourceMustExist(request);

        private Task<Validation<BaseError, PlexMediaSource>> PlexMediaSourceMustExist(
            UpdatePlexPathReplacements request) =>
            _mediaSourceRepository.GetPlex(request.PlexMediaSourceId)
                .Map(v => v.ToValidation<BaseError>($"Plex media source {request.PlexMediaSourceId} does not exist."));
    }
}
