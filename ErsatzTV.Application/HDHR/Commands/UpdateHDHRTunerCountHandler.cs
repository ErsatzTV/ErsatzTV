using System.Globalization;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.HDHR;

public class UpdateHDHRTunerCountHandler : IRequestHandler<UpdateHDHRTunerCount, Either<BaseError, Unit>>
{
    private readonly IConfigElementRepository _configElementRepository;

    public UpdateHDHRTunerCountHandler(IConfigElementRepository configElementRepository) =>
        _configElementRepository = configElementRepository;

    public Task<Either<BaseError, Unit>> Handle(
        UpdateHDHRTunerCount request,
        CancellationToken cancellationToken) =>
        Validate(request)
            .MapT(
                _ => _configElementRepository.Upsert(
                    ConfigElementKey.HDHRTunerCount,
                    request.TunerCount.ToString(CultureInfo.InvariantCulture)))
            .Bind(v => v.ToEitherAsync());

    private static Task<Validation<BaseError, Unit>> Validate(UpdateHDHRTunerCount request) =>
        Optional(request.TunerCount)
            .Where(tc => tc > 0)
            .Map(_ => Unit.Default)
            .ToValidation<BaseError>("Tuner count must be greater than zero")
            .AsTask();
}
