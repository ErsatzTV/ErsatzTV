using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Playouts.Commands
{
    public class DeletePlayoutHandler : IRequestHandler<DeletePlayout, Either<BaseError, Task>>
    {
        private readonly IPlayoutRepository _playoutRepository;

        public DeletePlayoutHandler(IPlayoutRepository playoutRepository) =>
            _playoutRepository = playoutRepository;

        public async Task<Either<BaseError, Task>> Handle(
            DeletePlayout request,
            CancellationToken cancellationToken) =>
            (await PlayoutMustExist(request))
            .Map(DoDeletion)
            .ToEither<Task>();

        private Task DoDeletion(int playoutId) => _playoutRepository.Delete(playoutId);

        private async Task<Validation<BaseError, int>> PlayoutMustExist(
            DeletePlayout deletePlayout) =>
            (await _playoutRepository.Get(deletePlayout.PlayoutId))
            .ToValidation<BaseError>($"Playout {deletePlayout.PlayoutId} does not exist.")
            .Map(c => c.Id);
    }
}
