using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Configuration;

public class GetPlayoutDaysToBuildHandler : IRequestHandler<GetPlayoutDaysToBuild, int>
{
    private readonly IConfigElementRepository _configElementRepository;

    public GetPlayoutDaysToBuildHandler(IConfigElementRepository configElementRepository) =>
        _configElementRepository = configElementRepository;

    public Task<int> Handle(GetPlayoutDaysToBuild request, CancellationToken cancellationToken) =>
        _configElementRepository.GetValue<int>(ConfigElementKey.PlayoutDaysToBuild)
            .Map(result => result.IfNone(2));
}