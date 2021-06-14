using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Watermarks.Mapper;

namespace ErsatzTV.Application.Watermarks.Queries
{
    public class GetWatermarkByIdHandler : IRequestHandler<GetWatermarkById, Option<WatermarkViewModel>>
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public GetWatermarkByIdHandler(IDbContextFactory<TvContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

        public async Task<Option<WatermarkViewModel>> Handle(
            GetWatermarkById request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.ChannelWatermarks
                .SelectOneAsync(w => w.Id, w => w.Id == request.Id)
                .MapT(ProjectToViewModel);
        }
    }
}
