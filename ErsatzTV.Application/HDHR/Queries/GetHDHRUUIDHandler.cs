using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.HDHR;

public class GetHDHRUUIDHandler : IRequestHandler<GetHDHRUUID, Guid>
{
    private readonly IConfigElementRepository _configElementRepository;

    public GetHDHRUUIDHandler(IConfigElementRepository configElementRepository) =>
        _configElementRepository = configElementRepository;

    public async Task<Guid> Handle(GetHDHRUUID request, CancellationToken cancellationToken)
    {
        Option<Guid> maybeGuid = await _configElementRepository.GetValue<Guid>(ConfigElementKey.HDHRUUID);
        return await maybeGuid.IfNoneAsync(
            async () =>
            {
                Guid guid = Guid.NewGuid();
                await _configElementRepository.Upsert(ConfigElementKey.HDHRUUID, guid);
                return guid;
            });
    }
}
