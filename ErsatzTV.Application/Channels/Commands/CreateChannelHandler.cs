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
    public class CreateChannelHandler : IRequestHandler<CreateChannel, Either<BaseError, ChannelViewModel>>
    {
        private readonly IChannelRepository _channelRepository;
        private readonly IFFmpegProfileRepository _ffmpegProfileRepository;

        public CreateChannelHandler(
            IChannelRepository channelRepository,
            IFFmpegProfileRepository ffmpegProfileRepository)
        {
            _channelRepository = channelRepository;
            _ffmpegProfileRepository = ffmpegProfileRepository;
        }

        public Task<Either<BaseError, ChannelViewModel>> Handle(
            CreateChannel request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(PersistChannel)
                .Bind(v => v.ToEitherAsync());

        private Task<ChannelViewModel> PersistChannel(Channel c) =>
            _channelRepository.Add(c).Map(ProjectToViewModel);

        private async Task<Validation<BaseError, Channel>> Validate(CreateChannel request) =>
            (ValidateName(request), await ValidateNumber(request), await FFmpegProfileMustExist(request),
                ValidatePreferredLanguage(request))
            .Apply(
                (name, number, ffmpegProfileId, preferredLanguageCode) =>
                {
                    var artwork = new List<Artwork>();
                    if (!string.IsNullOrWhiteSpace(request.Logo))
                    {
                        artwork.Add(
                            new Artwork
                            {
                                Path = request.Logo,
                                ArtworkKind = ArtworkKind.Logo,
                                DateAdded = DateTime.UtcNow,
                                DateUpdated = DateTime.UtcNow
                            });
                    }

                    return new Channel(Guid.NewGuid())
                    {
                        Name = name,
                        Number = number,
                        FFmpegProfileId = ffmpegProfileId,
                        StreamingMode = request.StreamingMode,
                        Artwork = artwork,
                        PreferredLanguageCode = preferredLanguageCode
                    };
                });

        private Validation<BaseError, string> ValidateName(CreateChannel createChannel) =>
            createChannel.NotEmpty(c => c.Name)
                .Bind(_ => createChannel.NotLongerThan(50)(c => c.Name));

        private Validation<BaseError, string> ValidatePreferredLanguage(CreateChannel createChannel) =>
            Optional(createChannel.PreferredLanguageCode)
                .Filter(
                    lc => string.IsNullOrWhiteSpace(lc) || CultureInfo.GetCultures(CultureTypes.NeutralCultures).Any(
                        ci => string.Equals(ci.ThreeLetterISOLanguageName, lc, StringComparison.OrdinalIgnoreCase)))
                .ToValidation<BaseError>("Preferred language code is invalid");


        private async Task<Validation<BaseError, string>> ValidateNumber(CreateChannel createChannel)
        {
            Option<Channel> maybeExistingChannel = await _channelRepository.GetByNumber(createChannel.Number);
            return maybeExistingChannel.Match<Validation<BaseError, string>>(
                _ => BaseError.New("Channel number must be unique"),
                () =>
                {
                    if (Regex.IsMatch(createChannel.Number, Channel.NumberValidator))
                    {
                        return createChannel.Number;
                    }

                    return BaseError.New("Invalid channel number; one decimal is allowed for subchannels");
                });
        }

        private async Task<Validation<BaseError, int>> FFmpegProfileMustExist(CreateChannel createChannel) =>
            (await _ffmpegProfileRepository.Get(createChannel.FFmpegProfileId))
            .ToValidation<BaseError>($"FFmpegProfile {createChannel.FFmpegProfileId} does not exist.")
            .Map(c => c.Id);
    }
}
