using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Playouts.Commands
{
    public class BuildPlayoutHandler : MediatR.IRequestHandler<BuildPlayout, Either<BaseError, Unit>>
    {
        private readonly IPlayoutBuilder _playoutBuilder;
        private readonly IPlayoutRepository _playoutRepository;

        public BuildPlayoutHandler(IPlayoutRepository playoutRepository, IPlayoutBuilder playoutBuilder)
        {
            _playoutRepository = playoutRepository;
            _playoutBuilder = playoutBuilder;
        }

        public Task<Either<BaseError, Unit>> Handle(BuildPlayout request, CancellationToken cancellationToken) =>
            Validate(request)
                .Map(v => v.ToEither<Playout>())
                .BindT(playout => ApplyUpdateRequest(request, playout));

        private async Task<Either<BaseError, Unit>> ApplyUpdateRequest(BuildPlayout request, Playout playout)
        {
            Playout result = await _playoutBuilder.BuildPlayoutItems(playout, request.Rebuild);
            await _playoutRepository.Update(result);
            return unit;
        }

        private Task<Validation<BaseError, Playout>> Validate(BuildPlayout request) =>
            PlayoutMustExist(request);

        private async Task<Validation<BaseError, Playout>> PlayoutMustExist(BuildPlayout buildPlayout) =>
            (await _playoutRepository.GetFull(buildPlayout.PlayoutId))
            .ToValidation<BaseError>("Playout does not exist.");
    }
}
