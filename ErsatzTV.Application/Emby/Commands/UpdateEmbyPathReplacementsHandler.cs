using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Emby;

public class UpdateEmbyPathReplacementsHandler : IRequestHandler<UpdateEmbyPathReplacements,
    Either<BaseError, Unit>>
{
    private readonly IMediaSourceRepository _mediaSourceRepository;

    public UpdateEmbyPathReplacementsHandler(IMediaSourceRepository mediaSourceRepository) =>
        _mediaSourceRepository = mediaSourceRepository;

    public Task<Either<BaseError, Unit>> Handle(
        UpdateEmbyPathReplacements request,
        CancellationToken cancellationToken) =>
        Validate(request, cancellationToken)
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

    private Task<Validation<BaseError, EmbyMediaSource>> Validate(UpdateEmbyPathReplacements request, CancellationToken cancellationToken) =>
        EmbyMediaSourceMustExist(request, cancellationToken);

    private Task<Validation<BaseError, EmbyMediaSource>> EmbyMediaSourceMustExist(
        UpdateEmbyPathReplacements request, CancellationToken cancellationToken) =>
        _mediaSourceRepository.GetEmby(request.EmbyMediaSourceId, cancellationToken)
            .Map(v => v.ToValidation<BaseError>(
                $"Emby media source {request.EmbyMediaSourceId} does not exist."));
}
