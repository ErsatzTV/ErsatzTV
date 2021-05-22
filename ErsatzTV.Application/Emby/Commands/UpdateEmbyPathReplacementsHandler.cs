using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.Emby.Commands
{
    public class UpdateEmbyPathReplacementsHandler : MediatR.IRequestHandler<UpdateEmbyPathReplacements,
        Either<BaseError, Unit>>
    {
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public UpdateEmbyPathReplacementsHandler(IMediaSourceRepository mediaSourceRepository) =>
            _mediaSourceRepository = mediaSourceRepository;

        public Task<Either<BaseError, Unit>> Handle(
            UpdateEmbyPathReplacements request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(pms => MergePathReplacements(request, pms))
                .Bind(v => v.ToEitherAsync());

        private Task<Unit> MergePathReplacements(
            UpdateEmbyPathReplacements request,
            EmbyMediaSource embyMediaSource)
        {
            embyMediaSource.PathReplacements ??= new List<EmbyPathReplacement>();

            var incoming = request.PathReplacements.Map(Project).ToList();

            var toAdd = incoming.Filter(r => r.Id < 1).ToList();
            var toRemove = embyMediaSource.PathReplacements.Filter(r => incoming.All(pr => pr.Id != r.Id)).ToList();
            var toUpdate = incoming.Except(toAdd).ToList();

            return _mediaSourceRepository.UpdatePathReplacements(embyMediaSource.Id, toAdd, toUpdate, toRemove);
        }

        private static EmbyPathReplacement Project(EmbyPathReplacementItem vm) =>
            new() { Id = vm.Id, EmbyPath = vm.EmbyPath, LocalPath = vm.LocalPath };

        private Task<Validation<BaseError, EmbyMediaSource>> Validate(UpdateEmbyPathReplacements request) =>
            EmbyMediaSourceMustExist(request);

        private Task<Validation<BaseError, EmbyMediaSource>> EmbyMediaSourceMustExist(
            UpdateEmbyPathReplacements request) =>
            _mediaSourceRepository.GetEmby(request.EmbyMediaSourceId)
                .Map(
                    v => v.ToValidation<BaseError>(
                        $"Emby media source {request.EmbyMediaSourceId} does not exist."));
    }
}
