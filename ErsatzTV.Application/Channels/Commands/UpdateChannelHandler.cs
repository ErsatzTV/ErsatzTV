using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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
            c.PreferredLanguageCode = update.PreferredLanguageCode;

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
            (await ChannelMustExist(request), ValidateName(request), await ValidateNumber(request),
                ValidatePreferredLanguage(request))
            .Apply((channelToUpdate, _, _, _) => channelToUpdate);

        private Task<Validation<BaseError, Channel>> ChannelMustExist(UpdateChannel updateChannel) =>
            _channelRepository.Get(updateChannel.ChannelId)
                .Map(v => v.ToValidation<BaseError>("Channel does not exist."));

        private Validation<BaseError, string> ValidateName(UpdateChannel updateChannel) =>
            updateChannel.NotEmpty(c => c.Name)
                .Bind(_ => updateChannel.NotLongerThan(50)(c => c.Name));

        private async Task<Validation<BaseError, string>> ValidateNumber(UpdateChannel updateChannel)
        {
            Option<Channel> match = await _channelRepository.GetByNumber(updateChannel.Number);
            int matchId = match.Map(c => c.Id).IfNone(updateChannel.ChannelId);
            if (matchId == updateChannel.ChannelId)
            {
                if (Regex.IsMatch(updateChannel.Number, Channel.NumberValidator))
                {
                    return updateChannel.Number;
                }

                return BaseError.New("Invalid channel number; one decimal is allowed for subchannels");
            }

            return BaseError.New("Channel number must be unique");
        }

        private Validation<BaseError, string> ValidatePreferredLanguage(UpdateChannel updateChannel) =>
            Optional(updateChannel.PreferredLanguageCode ?? string.Empty)
                .Filter(
                    lc => string.IsNullOrWhiteSpace(lc) || CultureInfo.GetCultures(CultureTypes.NeutralCultures).Any(
                        ci => string.Equals(ci.ThreeLetterISOLanguageName, lc, StringComparison.OrdinalIgnoreCase)))
                .ToValidation<BaseError>("Preferred language code is invalid");
    }
}
