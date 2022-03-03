using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Data;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Resolutions.Mapper;

namespace ErsatzTV.Application.Resolutions;

public class GetAllResolutionsHandler : IRequestHandler<GetAllResolutions, List<ResolutionViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetAllResolutionsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<ResolutionViewModel>> Handle(
        GetAllResolutions request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        return await dbContext.Resolutions
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());
    }
}