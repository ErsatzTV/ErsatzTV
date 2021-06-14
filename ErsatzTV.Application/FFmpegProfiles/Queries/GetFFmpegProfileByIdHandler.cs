using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.FFmpegProfiles.Mapper;

namespace ErsatzTV.Application.FFmpegProfiles.Queries
{
    public class GetFFmpegProfileByIdHandler : IRequestHandler<GetFFmpegProfileById, Option<FFmpegProfileViewModel>>
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public GetFFmpegProfileByIdHandler(IDbContextFactory<TvContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

        public async Task<Option<FFmpegProfileViewModel>> Handle(
            GetFFmpegProfileById request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.FFmpegProfiles
                .Include(p => p.Resolution)
                .SelectOneAsync(p => p.Id, p => p.Id == request.Id)
                .MapT(ProjectToViewModel);
        }
    }
}
