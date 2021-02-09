using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class ResolutionRepository : IResolutionRepository
    {
        private readonly TvContext _dbContext;

        public ResolutionRepository(TvContext dbContext) => _dbContext = dbContext;

        public Task<Option<Resolution>> Get(int id) =>
            _dbContext.Resolutions.SingleOrDefaultAsync(r => r.Id == id).Map(Optional);

        public Task<List<Resolution>> GetAll() =>
            _dbContext.Resolutions.ToListAsync();
    }
}
