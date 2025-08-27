using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Plex;

public class
    UpdatePlexPathReplacementsHandler : IRequestHandler<UpdatePlexPathReplacements, Either<BaseError, Unit>>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public UpdatePlexPathReplacementsHandler(IMediaSourceRepository mediaSourceRepository) =>
        _mediaSourceRepository = mediaSourceRepository;

    public Task<Either<BaseError, Unit>> Handle(
        UpdatePlexPathReplacements request,
        CancellationToken cancellationToken) =>
        Validate(request, cancellationToken)
            .MapT(pms => MergePathReplacements(request, pms))
            .Bind(v => v.ToEitherAsync());

    private Task<Unit> MergePathReplacements(UpdatePlexPathReplacements request, PlexMediaSource plexMediaSource)
    {
        plexMediaSource.PathReplacements ??= [];

        var incoming = request.PathReplacements.Map(Project).ToList();

        var toAdd = incoming.Filter(r => r.Id < 1).ToList();
        var toRemove = plexMediaSource.PathReplacements.Filter(r => incoming.All(pr => pr.Id != r.Id)).ToList();
        var toUpdate = incoming.Except(toAdd).ToList();

        return _mediaSourceRepository.UpdatePathReplacements(plexMediaSource.Id, toAdd, toUpdate, toRemove);
    }

    private static PlexPathReplacement Project(PlexPathReplacementItem vm) =>
        new() { Id = vm.Id, PlexPath = vm.PlexPath, LocalPath = vm.LocalPath };

    private Task<Validation<BaseError, PlexMediaSource>> Validate(
        UpdatePlexPathReplacements request,
        CancellationToken cancellationToken) =>
        PlexMediaSourceMustExist(request, cancellationToken);

    private Task<Validation<BaseError, PlexMediaSource>> PlexMediaSourceMustExist(
        UpdatePlexPathReplacements request,
        CancellationToken cancellationToken) =>
        _mediaSourceRepository.GetPlex(request.PlexMediaSourceId, cancellationToken)
            .Map(v => v.ToValidation<BaseError>($"Plex media source {request.PlexMediaSourceId} does not exist."));
}
