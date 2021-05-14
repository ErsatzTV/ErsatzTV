using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Configuration.Queries
{
    public class GetLibraryRefreshIntervalHandler : IRequestHandler<GetLibraryRefreshInterval, int>
    {
        private readonly IConfigElementRepository _configElementRepository;

        public GetLibraryRefreshIntervalHandler(IConfigElementRepository configElementRepository) =>
            _configElementRepository = configElementRepository;

        public Task<int> Handle(GetLibraryRefreshInterval request, CancellationToken cancellationToken) =>
            _configElementRepository.GetValue<int>(ConfigElementKey.LibraryRefreshInterval)
                .Map(result => result.IfNone(6));
    }
}
