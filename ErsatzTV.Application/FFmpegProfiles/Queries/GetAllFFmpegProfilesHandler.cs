using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using MediatR;
using static ErsatzTV.Application.FFmpegProfiles.Mapper;

namespace ErsatzTV.Application.FFmpegProfiles.Queries
{
    public class GetAllFFmpegProfilesHandler : IRequestHandler<GetAllFFmpegProfiles, List<FFmpegProfileViewModel>>
    {
        private readonly IFFmpegProfileRepository _ffmpegProfileRepository;

        public GetAllFFmpegProfilesHandler(IFFmpegProfileRepository ffmpegProfileRepository) =>
            _ffmpegProfileRepository = ffmpegProfileRepository;

        public async Task<List<FFmpegProfileViewModel>> Handle(
            GetAllFFmpegProfiles request,
            CancellationToken cancellationToken) =>
            (await _ffmpegProfileRepository.GetAll()).Map(ProjectToViewModel).ToList();
    }
}
