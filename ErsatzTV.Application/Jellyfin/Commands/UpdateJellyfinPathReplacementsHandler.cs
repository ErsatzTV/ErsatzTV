using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.Jellyfin;

public class UpdateJellyfinPathReplacementsHandler : MediatR.IRequestHandler<UpdateJellyfinPathReplacements,
    Either<BaseError, Unit>>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public UpdateJellyfinPathReplacementsHandler(IMediaSourceRepository mediaSourceRepository) =>
        _mediaSourceRepository = mediaSourceRepository;

    public Task<Either<BaseError, Unit>> Handle(
        UpdateJellyfinPathReplacements request,
        CancellationToken cancellationToken) =>
        Validate(request)
            .MapT(pms => MergePathReplacements(request, pms))
            .Bind(v => v.ToEitherAsync());

    private Task<Unit> MergePathReplacements(
        UpdateJellyfinPathReplacements request,
        JellyfinMediaSource jellyfinMediaSource)
    {
        jellyfinMediaSource.PathReplacements ??= new List<JellyfinPathReplacement>();

        var incoming = request.PathReplacements.Map(Project).ToList();

        var toAdd = incoming.Filter(r => r.Id < 1).ToList();
        var toRemove = jellyfinMediaSource.PathReplacements.Filter(r => incoming.All(pr => pr.Id != r.Id)).ToList();
        var toUpdate = incoming.Except(toAdd).ToList();

        return _mediaSourceRepository.UpdatePathReplacements(jellyfinMediaSource.Id, toAdd, toUpdate, toRemove);
    }

    private static JellyfinPathReplacement Project(JellyfinPathReplacementItem vm) =>
        new() { Id = vm.Id, JellyfinPath = vm.JellyfinPath, LocalPath = vm.LocalPath };

    private Task<Validation<BaseError, JellyfinMediaSource>> Validate(UpdateJellyfinPathReplacements request) =>
        JellyfinMediaSourceMustExist(request);

    private Task<Validation<BaseError, JellyfinMediaSource>> JellyfinMediaSourceMustExist(
        UpdateJellyfinPathReplacements request) =>
        _mediaSourceRepository.GetJellyfin(request.JellyfinMediaSourceId)
            .Map(
                v => v.ToValidation<BaseError>(
                    $"Jellyfin media source {request.JellyfinMediaSourceId} does not exist."));
}