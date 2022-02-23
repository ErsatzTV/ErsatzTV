using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.ProgramSchedules.Mapper;

namespace ErsatzTV.Application.ProgramSchedules.Queries
{
    public class GetProgramScheduleByIdHandler :
        IRequestHandler<GetProgramScheduleById, Option<ProgramScheduleViewModel>>
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public GetProgramScheduleByIdHandler(IDbContextFactory<TvContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

        public async Task<Option<ProgramScheduleViewModel>> Handle(
            GetProgramScheduleById request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await dbContext.ProgramSchedules
                .SelectOneAsync(ps => ps.Id, ps => ps.Id == request.Id)
                .MapT(ProjectToViewModel);
        }
    }
}
