using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;
using ValidationT_AsyncSync_Extensions = LanguageExt.ValidationT_AsyncSync_Extensions;

namespace ErsatzTV.Application.ProgramSchedules.Commands
{
    public class CreateProgramScheduleHandler :
        IRequestHandler<CreateProgramSchedule, Either<BaseError, CreateProgramScheduleResult>>
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public CreateProgramScheduleHandler(IDbContextFactory<TvContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

        public async Task<Either<BaseError, CreateProgramScheduleResult>> Handle(
            CreateProgramSchedule request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            Validation<BaseError, ProgramSchedule> validation = await Validate(dbContext, request);
            return await validation.Apply(ps => PersistProgramSchedule(dbContext, ps));
        }

        private static async Task<CreateProgramScheduleResult> PersistProgramSchedule(
            TvContext dbContext,
            ProgramSchedule programSchedule)
        {
            await dbContext.ProgramSchedules.AddAsync(programSchedule);
            await dbContext.SaveChangesAsync();
            return new CreateProgramScheduleResult(programSchedule.Id);
        }

        private static Task<Validation<BaseError, ProgramSchedule>> Validate(
            TvContext dbContext,
            CreateProgramSchedule request) =>
            ValidationT_AsyncSync_Extensions.MapT(
                ValidateName(dbContext, request),
                name =>
                {
                    bool keepMultiPartEpisodesTogether =
                        request.MediaCollectionPlaybackOrder == PlaybackOrder.Shuffle &&
                        request.KeepMultiPartEpisodesTogether;
                    return new ProgramSchedule
                    {
                        Name = name,
                        MediaCollectionPlaybackOrder = request.MediaCollectionPlaybackOrder,
                        KeepMultiPartEpisodesTogether = keepMultiPartEpisodesTogether,
                        TreatCollectionsAsShows = keepMultiPartEpisodesTogether && request.TreatCollectionsAsShows
                    };
                });

        private static async Task<Validation<BaseError, string>> ValidateName(
            TvContext dbContext,
            CreateProgramSchedule createProgramSchedule)
        {
            Validation<BaseError, string> result1 = createProgramSchedule.NotEmpty(c => c.Name)
                .Bind(_ => createProgramSchedule.NotLongerThan(50)(c => c.Name));

            int duplicateNameCount = await dbContext.ProgramSchedules
                .CountAsync(ps => ps.Name == createProgramSchedule.Name);

            var result2 = Optional(duplicateNameCount)
                .Filter(count => count == 0)
                .ToValidation<BaseError>("Schedule name must be unique");

            return (result1, result2).Apply((_, _) => createProgramSchedule.Name);
        }
    }
}
