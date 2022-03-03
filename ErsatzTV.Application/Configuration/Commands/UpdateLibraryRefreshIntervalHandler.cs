using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Configuration;

public class UpdateLibraryRefreshIntervalHandler :
    MediatR.IRequestHandler<UpdateLibraryRefreshInterval, Either<BaseError, Unit>>
{
    private readonly IConfigElementRepository _configElementRepository;

    public UpdateLibraryRefreshIntervalHandler(IConfigElementRepository configElementRepository) =>
        _configElementRepository = configElementRepository;

    public Task<Either<BaseError, Unit>> Handle(
        UpdateLibraryRefreshInterval request,
        CancellationToken cancellationToken) =>
        Validate(request)
            .MapT(_ => _configElementRepository.Upsert(ConfigElementKey.LibraryRefreshInterval, request.LibraryRefreshInterval))
            .Bind(v => v.ToEitherAsync());

    private static Task<Validation<BaseError, Unit>> Validate(UpdateLibraryRefreshInterval request) =>
        Optional(request.LibraryRefreshInterval)
            .Where(lri => lri > 0)
            .Map(_ => Unit.Default)
            .ToValidation<BaseError>("Tuner count must be greater than zero")
            .AsTask();
}