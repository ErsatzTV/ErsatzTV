using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.Configuration.Commands
{
    public class SaveConfigElementByKeyHandler : MediatR.IRequestHandler<SaveConfigElementByKey, Unit>
    {
        private readonly IConfigElementRepository _configElementRepository;

        public SaveConfigElementByKeyHandler(IConfigElementRepository configElementRepository) =>
            _configElementRepository = configElementRepository;

        public async Task<Unit> Handle(SaveConfigElementByKey request, CancellationToken cancellationToken)
        {
            await _configElementRepository.Upsert(request.Key, request.Value);
            return Unit.Default;
        }
    }
}
