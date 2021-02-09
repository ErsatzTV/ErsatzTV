using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Resolutions.Mapper;

namespace ErsatzTV.Application.Resolutions.Queries
{
    public class GetAllResolutionsHandler : IRequestHandler<GetAllResolutions, List<ResolutionViewModel>>
    {
        private readonly IResolutionRepository _resolutionRepository;

        public GetAllResolutionsHandler(IResolutionRepository resolutionRepository) =>
            _resolutionRepository = resolutionRepository;

        public Task<List<ResolutionViewModel>> Handle(GetAllResolutions request, CancellationToken cancellationToken) =>
            _resolutionRepository.GetAll().Map(resolutions => resolutions.Map(ProjectToViewModel).ToList());
    }
}
