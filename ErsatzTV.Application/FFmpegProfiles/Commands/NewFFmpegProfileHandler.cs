using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static LanguageExt.Prelude;
using static ErsatzTV.Application.FFmpegProfiles.Mapper;

namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    public class NewFFmpegProfileHandler : IRequestHandler<NewFFmpegProfile, FFmpegProfileViewModel>
    {
        private readonly IConfigElementRepository _configElementRepository;
        private readonly IResolutionRepository _resolutionRepository;

        public NewFFmpegProfileHandler(
            IResolutionRepository resolutionRepository,
            IConfigElementRepository configElementRepository)
        {
            _resolutionRepository = resolutionRepository;
            _configElementRepository = configElementRepository;
        }

        public async Task<FFmpegProfileViewModel> Handle(NewFFmpegProfile request, CancellationToken cancellationToken)
        {
            int defaultResolutionId = await _configElementRepository
                .GetValue<int>(ConfigElementKey.FFmpegDefaultResolutionId)
                .IfNoneAsync(0);

            List<Resolution> allResolutions = await _resolutionRepository.GetAll();

            Option<Resolution> maybeDefaultResolution = allResolutions.Find(r => r.Id == defaultResolutionId);
            Resolution defaultResolution = maybeDefaultResolution.Match(identity, () => allResolutions.Head());

            return ProjectToViewModel(FFmpegProfile.New("New Profile", defaultResolution));
        }
    }
}
