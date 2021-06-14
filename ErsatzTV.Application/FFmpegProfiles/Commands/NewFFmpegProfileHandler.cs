using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;
using static ErsatzTV.Application.FFmpegProfiles.Mapper;

namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    public class NewFFmpegProfileHandler : IRequestHandler<NewFFmpegProfile, FFmpegProfileViewModel>
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public NewFFmpegProfileHandler(IDbContextFactory<TvContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

        public async Task<FFmpegProfileViewModel> Handle(NewFFmpegProfile request, CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            int defaultResolutionId = await dbContext.ConfigElements
                .GetValue<int>(ConfigElementKey.FFmpegDefaultResolutionId)
                .IfNoneAsync(0);

            List<Resolution> allResolutions = await dbContext.Resolutions
                .ToListAsync(cancellationToken);

            Option<Resolution> maybeDefaultResolution = allResolutions.Find(r => r.Id == defaultResolutionId);
            Resolution defaultResolution = maybeDefaultResolution.Match(identity, () => allResolutions.Head());

            return ProjectToViewModel(FFmpegProfile.New("New Profile", defaultResolution));
        }
    }
}
