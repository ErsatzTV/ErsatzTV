using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Configuration.Mapper;

namespace ErsatzTV.Application.Configuration.Queries
{
    public class GetConfigElementByKeyHandler : IRequestHandler<GetConfigElementByKey, Option<ConfigElementViewModel>>
    {
        private readonly IConfigElementRepository _configElementRepository;

        public GetConfigElementByKeyHandler(IConfigElementRepository configElementRepository) =>
            _configElementRepository = configElementRepository;

        public Task<Option<ConfigElementViewModel>> Handle(
            GetConfigElementByKey request,
            CancellationToken cancellationToken) =>
            _configElementRepository.Get(request.Key).MapT(ProjectToViewModel);
    }
}
