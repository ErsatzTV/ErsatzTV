using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
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
            Option<ConfigElement> maybeElement = await _configElementRepository.Get(request.Key);
            await maybeElement.Match(
                ce =>
                {
                    ce.Value = request.Value;
                    return _configElementRepository.Update(ce);
                },
                () =>
                {
                    var ce = new ConfigElement { Key = request.Key.Key, Value = request.Value };
                    return _configElementRepository.Add(ce);
                });

            return Unit.Default;
        }
    }
}
