﻿using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using LanguageExt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;
using Channel = ErsatzTV.Core.Domain.Channel;

namespace ErsatzTV.Application.Playouts.Commands
{
    public class CreatePlayoutHandler : IRequestHandler<CreatePlayout, Either<BaseError, CreatePlayoutResponse>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public CreatePlayoutHandler(
            ChannelWriter<IBackgroundServiceRequest> channel,
            IDbContextFactory<TvContext> dbContextFactory)
        {
            _channel = channel;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<Either<BaseError, CreatePlayoutResponse>> Handle(
            CreatePlayout request,
            CancellationToken cancellationToken)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();

            Validation<BaseError, Playout> validation = await Validate(dbContext, request);
            return await validation.Apply(playout => PersistPlayout(dbContext, playout));
        }

        private async Task<CreatePlayoutResponse> PersistPlayout(TvContext dbContext, Playout playout)
        {
            await dbContext.Playouts.AddAsync(playout);
            await dbContext.SaveChangesAsync();
            await _channel.WriteAsync(new BuildPlayout(playout.Id));
            return new CreatePlayoutResponse(playout.Id);
        }

        private async Task<Validation<BaseError, Playout>> Validate(TvContext dbContext, CreatePlayout request) =>
            (await ValidateChannel(dbContext, request), await ValidateProgramSchedule(dbContext, request),
                ValidatePlayoutType(request))
            .Apply(
                (channel, programSchedule, playoutType) => new Playout
                {
                    ChannelId = channel.Id,
                    ProgramScheduleId = programSchedule.Id,
                    ProgramSchedulePlayoutType = playoutType
                });

        private static Task<Validation<BaseError, Channel>> ValidateChannel(
            TvContext dbContext,
            CreatePlayout createPlayout) =>
            dbContext.Channels
                .Include(c => c.Playouts)
                .Filter(c => c.Id == createPlayout.ChannelId)
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync()
                .Map(Optional)
                .Map(o => o.ToValidation<BaseError>("Channel does not exist"))
                .BindT(ChannelMustNotHavePlayouts);

        private static Validation<BaseError, Channel> ChannelMustNotHavePlayouts(Channel channel) =>
            Optional(channel.Playouts.Count)
                .Filter(count => count == 0)
                .Map(_ => channel)
                .ToValidation<BaseError>("Channel already has one playout");

        private static Task<Validation<BaseError, ProgramSchedule>> ValidateProgramSchedule(
            TvContext dbContext,
            CreatePlayout createPlayout) =>
            dbContext.ProgramSchedules
                .Include(ps => ps.Items)
                .Filter(ps => ps.Id == createPlayout.ProgramScheduleId)
                .OrderBy(ps => ps.Id)
                .FirstOrDefaultAsync()
                .Map(Optional)
                .Map(o => o.ToValidation<BaseError>("Program schedule does not exist"))
                .BindT(ProgramScheduleMustHaveItems);

        private static Validation<BaseError, ProgramSchedule> ProgramScheduleMustHaveItems(
            ProgramSchedule programSchedule) =>
            Optional(programSchedule)
                .Filter(ps => ps.Items.Any())
                .ToValidation<BaseError>("Program schedule must have items");

        private static Validation<BaseError, ProgramSchedulePlayoutType> ValidatePlayoutType(
            CreatePlayout createPlayout) =>
            Optional(createPlayout.ProgramSchedulePlayoutType)
                .Filter(playoutType => playoutType != ProgramSchedulePlayoutType.None)
                .ToValidation<BaseError>("[ProgramSchedulePlayoutType] must not be None");
    }
}
