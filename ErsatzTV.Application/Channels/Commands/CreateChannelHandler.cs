using System.Text.RegularExpressions;
using System.Threading.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Channel = ErsatzTV.Core.Domain.Channel;

namespace ErsatzTV.Application.Channels;

public class CreateChannelHandler(
    ChannelWriter<IBackgroundServiceRequest> workerChannel,
    IDbContextFactory<TvContext> dbContextFactory,
    ISearchTargets searchTargets)
    : IRequestHandler<CreateChannel, Either<BaseError, CreateChannelResult>>
{
    public async Task<Either<BaseError, CreateChannelResult>> Handle(
        CreateChannel request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Channel> validation = await Validate(dbContext, request);
        return await validation.Apply(c => PersistChannel(dbContext, c));
    }

    private async Task<CreateChannelResult> PersistChannel(TvContext dbContext, Channel channel)
    {
        await dbContext.Channels.AddAsync(channel);
        await dbContext.SaveChangesAsync();
        searchTargets.SearchTargetsChanged();
        await workerChannel.WriteAsync(new RefreshChannelList());
        return new CreateChannelResult(channel.Id);
    }

    private static async Task<Validation<BaseError, Channel>> Validate(TvContext dbContext, CreateChannel request) =>
        (ValidateName(request), await ValidateNumber(dbContext, request),
            await FFmpegProfileMustExist(dbContext, request),
            await WatermarkMustExist(dbContext, request),
            await FillerPresetMustExist(dbContext, request))
        .Apply((
            name,
            number,
            ffmpegProfileId,
            watermarkId,
            fillerPresetId) =>
        {
            var artwork = new List<Artwork>();
            if (!string.IsNullOrWhiteSpace(request.Logo?.Path))
            {
                string logo = request.Logo.Path;
                if (logo.StartsWith("iptv/logos/", StringComparison.Ordinal))
                {
                    logo = logo.Replace("iptv/logos/", string.Empty);
                }

                artwork.Add(
                    new Artwork
                    {
                        Path = logo,
                        ArtworkKind = ArtworkKind.Logo,
                        OriginalContentType = !string.IsNullOrEmpty(request.Logo.ContentType)
                            ? request.Logo.ContentType
                            : null,
                        DateAdded = DateTime.UtcNow,
                        DateUpdated = DateTime.UtcNow
                    });
            }

            var channel = new Channel(Guid.NewGuid())
            {
                Name = name,
                Number = number,
                Group = request.Group,
                Categories = request.Categories,
                FFmpegProfileId = ffmpegProfileId,
                PlayoutMode = request.PlayoutMode,
                StreamingMode = request.StreamingMode,
                Artwork = artwork,
                StreamSelectorMode = request.StreamSelectorMode,
                StreamSelector = request.StreamSelector,
                PreferredAudioLanguageCode = request.PreferredAudioLanguageCode,
                PreferredAudioTitle = request.PreferredAudioTitle,
                PreferredSubtitleLanguageCode = request.PreferredSubtitleLanguageCode,
                SubtitleMode = request.SubtitleMode,
                MusicVideoCreditsMode = request.MusicVideoCreditsMode,
                MusicVideoCreditsTemplate = request.MusicVideoCreditsTemplate,
                SongVideoMode = request.SongVideoMode,
                ActiveMode = request.ActiveMode,
                TranscodeMode = request.TranscodeMode,
                IdleBehavior = request.IdleBehavior,
                IsEnabled = request.IsEnabled,
                ShowInEpg = request.ShowInEpg
            };

            foreach (int id in watermarkId)
            {
                channel.WatermarkId = id;
            }

            foreach (int id in fillerPresetId)
            {
                channel.FallbackFillerId = id;
            }

            return channel;
        });

    private static Validation<BaseError, string> ValidateName(CreateChannel createChannel) =>
        createChannel.NotEmpty(c => c.Name)
            .Bind(_ => createChannel.NotLongerThan(50)(c => c.Name));

    private static async Task<Validation<BaseError, string>> ValidateNumber(
        TvContext dbContext,
        CreateChannel createChannel)
    {
        Option<Channel> maybeExistingChannel = await dbContext.Channels
            .SelectOneAsync(c => c.Number, c => c.Number == createChannel.Number);
        return maybeExistingChannel.Match<Validation<BaseError, string>>(
            _ => BaseError.New("Channel number must be unique"),
            () =>
            {
                if (Regex.IsMatch(createChannel.Number, Channel.NumberValidator))
                {
                    return createChannel.Number;
                }

                return BaseError.New("Invalid channel number; two decimals are allowed for subchannels");
            });
    }

    private static Task<Validation<BaseError, int>> FFmpegProfileMustExist(
        TvContext dbContext,
        CreateChannel createChannel) =>
        dbContext.FFmpegProfiles
            .CountAsync(p => p.Id == createChannel.FFmpegProfileId)
            .Map(Optional)
            .Filter(c => c > 0)
            .MapT(_ => createChannel.FFmpegProfileId)
            .Map(o => o.ToValidation<BaseError>($"FFmpegProfile {createChannel.FFmpegProfileId} does not exist."));

    private static async Task<Validation<BaseError, Option<int>>> WatermarkMustExist(
        TvContext dbContext,
        CreateChannel createChannel)
    {
        if (createChannel.WatermarkId is null)
        {
            return Option<int>.None;
        }

        return await dbContext.ChannelWatermarks
            .CountAsync(w => w.Id == createChannel.WatermarkId)
            .Map(Optional)
            .Filter(c => c > 0)
            .MapT(_ => Optional(createChannel.WatermarkId))
            .Map(o => o.ToValidation<BaseError>($"Watermark {createChannel.WatermarkId} does not exist."));
    }

    private static async Task<Validation<BaseError, Option<int>>> FillerPresetMustExist(
        TvContext dbContext,
        CreateChannel createChannel)
    {
        if (createChannel.FallbackFillerId is null)
        {
            return Option<int>.None;
        }

        return await dbContext.FillerPresets
            .Filter(fp => fp.FillerKind == FillerKind.Fallback)
            .CountAsync(w => w.Id == createChannel.FallbackFillerId)
            .Map(Optional)
            .Filter(c => c > 0)
            .MapT(_ => Optional(createChannel.FallbackFillerId))
            .Map(o => o.ToValidation<BaseError>(
                $"Fallback filler {createChannel.FallbackFillerId} does not exist."));
    }
}
