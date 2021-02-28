using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Channels.Mapper;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.Channels.Commands
{
    public class UpdateChannelHandler : IRequestHandler<UpdateChannel, Either<BaseError, ChannelViewModel>>
    {
        private readonly IChannelRepository _channelRepository;

        public UpdateChannelHandler(IChannelRepository channelRepository) => _channelRepository = channelRepository;

        public Task<Either<BaseError, ChannelViewModel>> Handle(
            UpdateChannel request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(c => ApplyUpdateRequest(c, request))
                .Bind(v => v.ToEitherAsync());

        private async Task<ChannelViewModel> ApplyUpdateRequest(Channel c, UpdateChannel update)
        {
            c.Name = update.Name;
            c.Number = update.Number;
            c.FFmpegProfileId = update.FFmpegProfileId;

            if (!string.IsNullOrWhiteSpace(update.Logo))
            {
                c.Artwork ??= new List<Artwork>();

                Option<Artwork> maybeLogo =
                    Optional(c.Artwork).Flatten().FirstOrDefault(a => a.ArtworkKind == ArtworkKind.Logo);

                maybeLogo.Match(
                    artwork =>
                    {
                        artwork.Path = update.Logo;
                        artwork.DateUpdated = DateTime.UtcNow;
                    },
                    () =>
                    {
                        var artwork = new Artwork
                        {
                            Path = update.Logo,
                            DateAdded = DateTime.UtcNow,
                            DateUpdated = DateTime.UtcNow,
                            ArtworkKind = ArtworkKind.Logo
                        };
                        c.Artwork.Add(artwork);
                    });
            }

            c.StreamingMode = update.StreamingMode;
            await _channelRepository.Update(c);
            return ProjectToViewModel(c);
        }

        private async Task<Validation<BaseError, Channel>> Validate(UpdateChannel request) =>
            (await ChannelMustExist(request), ValidateName(request), await ValidateNumber(request))
            .Apply((channelToUpdate, _, _) => channelToUpdate);

        private Task<Validation<BaseError, Channel>> ChannelMustExist(UpdateChannel updateChannel) =>
            _channelRepository.Get(updateChannel.ChannelId)
                .Map(v => v.ToValidation<BaseError>("Channel does not exist."));

        private Validation<BaseError, string> ValidateName(UpdateChannel updateChannel) =>
            updateChannel.NotEmpty(c => c.Name)
                .Bind(_ => updateChannel.NotLongerThan(50)(c => c.Name));

        private async Task<Validation<BaseError, int>> ValidateNumber(UpdateChannel updateChannel)
        {
            Option<Channel> match = await _channelRepository.GetByNumber(updateChannel.Number);
            int matchId = match.Map(c => c.Id).IfNone(updateChannel.ChannelId);
            if (matchId == updateChannel.ChannelId)
            {
                return updateChannel.AtLeast(1)(c => c.Number);
            }

            return BaseError.New("Channel number must be unique");
        }
    }
}
