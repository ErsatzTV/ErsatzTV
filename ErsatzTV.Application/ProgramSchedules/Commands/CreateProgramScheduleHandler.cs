using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.ProgramSchedules.Mapper;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.ProgramSchedules.Commands
{
    public class
        CreateProgramScheduleHandler : IRequestHandler<CreateProgramSchedule,
            Either<BaseError, ProgramScheduleViewModel>>
    {
        private readonly IProgramScheduleRepository _programScheduleRepository;

        public CreateProgramScheduleHandler(IProgramScheduleRepository programScheduleRepository) =>
            _programScheduleRepository = programScheduleRepository;

        public Task<Either<BaseError, ProgramScheduleViewModel>> Handle(
            CreateProgramSchedule request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(PersistProgramSchedule)
                .Bind(v => v.ToEitherAsync());

        private Task<ProgramScheduleViewModel> PersistProgramSchedule(ProgramSchedule c) =>
            _programScheduleRepository.Add(c).Map(ProjectToViewModel);

        private Task<Validation<BaseError, ProgramSchedule>> Validate(CreateProgramSchedule request) =>
            ValidateName(request)
                .MapT(
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

        private async Task<Validation<BaseError, string>> ValidateName(CreateProgramSchedule createProgramSchedule)
        {
            List<string> allNames = await _programScheduleRepository.GetAll()
                .Map(list => list.Map(c => c.Name).ToList());

            Validation<BaseError, string> result1 = createProgramSchedule.NotEmpty(c => c.Name)
                .Bind(_ => createProgramSchedule.NotLongerThan(50)(c => c.Name));

            var result2 = Optional(createProgramSchedule.Name)
                .Filter(name => !allNames.Contains(name))
                .ToValidation<BaseError>("Schedule name must be unique");

            return (result1, result2).Apply((_, _) => createProgramSchedule.Name);
        }
    }
}
