using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Configuration;

public class SaveConfigElementByKeyHandler : IRequestHandler<SaveConfigElementByKey>
{
    private readonly IConfigElementRepository _configElementRepository;

    public SaveConfigElementByKeyHandler(IConfigElementRepository configElementRepository) =>
        _configElementRepository = configElementRepository;

    public async Task Handle(SaveConfigElementByKey request, CancellationToken cancellationToken)
    {
        await _configElementRepository.Upsert(request.Key, request.Value);
    }
}
