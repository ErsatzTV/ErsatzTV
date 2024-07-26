using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.HDHR;

public class GetHDHRUUIDHandler : IRequestHandler<GetHDHRUUID, Guid>
{
    private readonly IConfigElementRepository _configElementRepository;

    public GetHDHRUUIDHandler(IConfigElementRepository configElementRepository) =>
        _configElementRepository = configElementRepository;

    public Task<Guid> Handle(GetHDHRUUID request, CancellationToken cancellationToken) =>
        _configElementRepository.GetValue<Guid>(ConfigElementKey.HDHRUUID)
            .Map(result => result.IfNone(() =>
                {
                    Guid guid = Guid.NewGuid();
                    var tmp = _configElementRepository.Upsert<Guid>(ConfigElementKey.HDHRUUID, guid);
                    return guid;
                })
            );
}
