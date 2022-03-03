using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Filler.Mapper;

namespace ErsatzTV.Application.Filler;

public class GetFillerPresetByIdHandler : IRequestHandler<GetFillerPresetById, Option<FillerPresetViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetFillerPresetByIdHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Option<FillerPresetViewModel>> Handle(
        GetFillerPresetById request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        return await dbContext.FillerPresets
            .SelectOneAsync(c => c.Id, c => c.Id == request.Id)
            .MapT(ProjectToViewModel);
    }
}