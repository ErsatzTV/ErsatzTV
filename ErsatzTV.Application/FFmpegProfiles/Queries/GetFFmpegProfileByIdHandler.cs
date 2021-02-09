using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.FFmpegProfiles.Mapper;

namespace ErsatzTV.Application.FFmpegProfiles.Queries
{
    public class GetFFmpegProfileByIdHandler : IRequestHandler<GetFFmpegProfileById, Option<FFmpegProfileViewModel>>
    {
        private readonly IFFmpegProfileRepository _ffmpegProfileRepository;

        public GetFFmpegProfileByIdHandler(IFFmpegProfileRepository ffmpegProfileRepository) =>
            _ffmpegProfileRepository = ffmpegProfileRepository;

        public Task<Option<FFmpegProfileViewModel>> Handle(
            GetFFmpegProfileById request,
            CancellationToken cancellationToken) =>
            _ffmpegProfileRepository.Get(request.Id)
                .MapT(ProjectToViewModel);
    }
}
