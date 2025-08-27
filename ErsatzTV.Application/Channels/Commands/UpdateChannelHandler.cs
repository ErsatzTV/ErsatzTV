using System.Text.RegularExpressions;
using System.Threading.Channels;
using ErsatzTV.Application.Subtitles;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Channels.Mapper;
using Channel = ErsatzTV.Core.Domain.Channel;

namespace ErsatzTV.Application.Channels;

public class UpdateChannelHandler(
    ChannelWriter<IBackgroundServiceRequest> workerChannel,
    IDbContextFactory<TvContext> dbContextFactory,
    ISearchTargets searchTargets)
    : IRequestHandler<UpdateChannel, Either<BaseError, ChannelViewModel>>
{
    public async Task<Either<BaseError, ChannelViewModel>> Handle(
        UpdateChannel request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Channel> validation = await Validate(dbContext, request, cancellationToken);
        return await validation.Apply(c => ApplyUpdateRequest(dbContext, c, request, cancellationToken));
    }

    private async Task<ChannelViewModel> ApplyUpdateRequest(
        TvContext dbContext,
        Channel c,
        UpdateChannel update,
        CancellationToken cancellationToken)
    {
        c.Name = update.Name;
        c.Number = update.Number;
        c.Group = update.Group;
        c.Categories = update.Categories;
        c.FFmpegProfileId = update.FFmpegProfileId;
        c.StreamSelectorMode = update.StreamSelectorMode;
        c.StreamSelector = update.StreamSelector;
        c.PreferredAudioLanguageCode = update.PreferredAudioLanguageCode;
        c.PreferredAudioTitle = update.PreferredAudioTitle;
        c.PreferredSubtitleLanguageCode = update.PreferredSubtitleLanguageCode;
        c.SubtitleMode = update.SubtitleMode;
        c.MusicVideoCreditsMode = update.MusicVideoCreditsMode;
        c.MusicVideoCreditsTemplate = update.MusicVideoCreditsTemplate;
        c.SongVideoMode = update.SongVideoMode;
        c.TranscodeMode = update.TranscodeMode;
        c.IdleBehavior = update.IdleBehavior;
        c.IsEnabled = update.IsEnabled;
        c.ShowInEpg = update.IsEnabled && update.ShowInEpg;
        c.Artwork ??= [];

        if (!string.IsNullOrWhiteSpace(update.Logo?.Path))
        {
            string logo = update.Logo.Path;
            if (logo.StartsWith("iptv/logos/", StringComparison.Ordinal))
            {
                logo = logo.Replace("iptv/logos/", string.Empty);
            }

            Option<Artwork> maybeLogo = c.Artwork.Where(a => a.ArtworkKind == ArtworkKind.Logo).HeadOrNone();
            foreach (Artwork artwork in maybeLogo)
            {
                artwork.Path = logo;
                artwork.OriginalContentType = !string.IsNullOrEmpty(update.Logo.ContentType)
                    ? update.Logo.ContentType
                    : null;
                artwork.DateUpdated = DateTime.UtcNow;
            }

            if (maybeLogo.IsNone)
            {
                var artwork = new Artwork
                {
                    Path = logo,
                    OriginalContentType = !string.IsNullOrEmpty(update.Logo.ContentType)
                        ? update.Logo.ContentType
                        : null,
                    DateAdded = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow,
                    ArtworkKind = ArtworkKind.Logo
                };
                c.Artwork.Add(artwork);
            }
        }
        else
        {
            await dbContext.Entry(c)
                .Collection(channel => channel.Artwork)
                .LoadAsync(cancellationToken);

            foreach (Artwork artwork in c.Artwork.Where(x => x.ArtworkKind is ArtworkKind.Logo).ToList())
            {
                c.Artwork.Remove(artwork);
                dbContext.Artwork.Remove(artwork);
            }
        }

        c.PlayoutMode = update.PlayoutMode;
        c.StreamingMode = update.StreamingMode;
        c.WatermarkId = update.WatermarkId;
        c.FallbackFillerId = update.FallbackFillerId;
        await dbContext.SaveChangesAsync(cancellationToken);

        searchTargets.SearchTargetsChanged();

        if (c.SubtitleMode != ChannelSubtitleMode.None)
        {
            Option<Playout> maybePlayout = await dbContext.Playouts
                .SelectOneAsync(p => p.ChannelId, p => p.ChannelId == c.Id, cancellationToken);

            foreach (Playout playout in maybePlayout)
            {
                await workerChannel.WriteAsync(new ExtractEmbeddedSubtitles(playout.Id), cancellationToken);
            }
        }

        await workerChannel.WriteAsync(new RefreshChannelList(), cancellationToken);

        return ProjectToViewModel(c);
    }

    private static async Task<Validation<BaseError, Channel>> Validate(
        TvContext dbContext,
        UpdateChannel request,
        CancellationToken cancellationToken) =>
        (await ChannelMustExist(dbContext, request, cancellationToken), ValidateName(request),
            await ValidateNumber(dbContext, request, cancellationToken))
        .Apply((channelToUpdate, _, _) => channelToUpdate);

    private static Task<Validation<BaseError, Channel>> ChannelMustExist(
        TvContext dbContext,
        UpdateChannel updateChannel,
        CancellationToken cancellationToken) =>
        dbContext.Channels
            .Include(c => c.Artwork)
            .Include(c => c.Watermark)
            .SelectOneAsync(c => c.Id, c => c.Id == updateChannel.ChannelId, cancellationToken)
            .Map(o => o.ToValidation<BaseError>("Channel does not exist."));

    private static Validation<BaseError, string> ValidateName(UpdateChannel updateChannel) =>
        updateChannel.NotEmpty(c => c.Name)
            .Bind(_ => updateChannel.NotLongerThan(50)(c => c.Name));

    private static async Task<Validation<BaseError, string>> ValidateNumber(
        TvContext dbContext,
        UpdateChannel updateChannel,
        CancellationToken cancellationToken)
    {
        int matchId = await dbContext.Channels
            .SelectOneAsync(c => c.Number, c => c.Number == updateChannel.Number, cancellationToken)
            .Match(c => c.Id, () => updateChannel.ChannelId);

        if (matchId == updateChannel.ChannelId)
        {
            if (Regex.IsMatch(updateChannel.Number, Channel.NumberValidator))
            {
                return updateChannel.Number;
            }

            return BaseError.New("Invalid channel number; two decimals are allowed for subchannels");
        }

        return BaseError.New("Channel number must be unique");
    }
}
