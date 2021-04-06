using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.HDHR.Queries
{
    public class GetHDHRTunerCountHandler : IRequestHandler<GetHDHRTunerCount, int>
    {
        private readonly IConfigElementRepository _configElementRepository;

        public GetHDHRTunerCountHandler(IConfigElementRepository configElementRepository) =>
            _configElementRepository = configElementRepository;

        public Task<int> Handle(GetHDHRTunerCount request, CancellationToken cancellationToken) =>
            _configElementRepository.GetValue<int>(ConfigElementKey.HDHRTunerCount)
                .Map(result => result.IfNone(2));
    }
}
