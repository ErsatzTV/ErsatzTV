using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LanguageExt;
using static ErsatzTV.Application.Filler.Mapper;

namespace ErsatzTV.Application.Filler.Queries
{
    public class GetAllFillerPresetsHandler : IRequestHandler<GetAllFillerPresets, List<FillerPresetViewModel>>
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public GetAllFillerPresetsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

        public async Task<List<FillerPresetViewModel>> Handle(
            GetAllFillerPresets request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.FillerPresets.ToListAsync(cancellationToken)
                .Map(presets => presets.Map(ProjectToViewModel).ToList());
        }
    }
}
