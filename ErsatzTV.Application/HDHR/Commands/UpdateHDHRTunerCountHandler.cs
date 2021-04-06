using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.HDHR.Commands
{
    public class UpdateHDHRTunerCountHandler : MediatR.IRequestHandler<UpdateHDHRTunerCount, Either<BaseError, Unit>>
    {
        private readonly IConfigElementRepository _configElementRepository;

        public UpdateHDHRTunerCountHandler(IConfigElementRepository configElementRepository) =>
            _configElementRepository = configElementRepository;

        public Task<Either<BaseError, Unit>> Handle(
            UpdateHDHRTunerCount request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(_ => Upsert(ConfigElementKey.HDHRTunerCount, request.TunerCount.ToString()))
                .Bind(v => v.ToEitherAsync());

        private Task<Validation<BaseError, Unit>> Validate(UpdateHDHRTunerCount request) =>
            Optional(request.TunerCount)
                .Filter(tc => tc > 0)
                .Map(_ => Unit.Default)
                .ToValidation<BaseError>("Tuner count must be greater than zero")
                .AsTask();

        private Task<Unit> Upsert(ConfigElementKey key, string value) =>
            _configElementRepository.Get(key).Match(
                ce =>
                {
                    ce.Value = value;
                    return _configElementRepository.Update(ce);
                },
                () =>
                {
                    var ce = new ConfigElement { Key = key.Key, Value = value };
                    return _configElementRepository.Add(ce);
                }).ToUnit();
    }
}
