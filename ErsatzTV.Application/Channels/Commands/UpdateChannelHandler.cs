﻿using System.Globalization;
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
        Validation<BaseError, Channel> validation = await Validate(dbContext, request);
        return await validation.Apply(c => ApplyUpdateRequest(dbContext, c, request));
    }

    private async Task<ChannelViewModel> ApplyUpdateRequest(TvContext dbContext, Channel c, UpdateChannel update)
    {
        c.Name = update.Name;
        c.Number = update.Number;
        c.Group = update.Group;
        c.Categories = update.Categories;
        c.FFmpegProfileId = update.FFmpegProfileId;
        c.PreferredAudioLanguageCode = update.PreferredAudioLanguageCode;
        c.PreferredAudioTitle = update.PreferredAudioTitle;
        c.PreferredSubtitleLanguageCode = update.PreferredSubtitleLanguageCode;
        c.SubtitleMode = update.SubtitleMode;
        c.MusicVideoCreditsMode = update.MusicVideoCreditsMode;
        c.MusicVideoCreditsTemplate = update.MusicVideoCreditsTemplate;
        c.SongVideoMode = update.SongVideoMode;
        c.Artwork ??= new List<Artwork>();

        if (!string.IsNullOrWhiteSpace(update.Logo))
        {
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

        c.ProgressMode = update.ProgressMode;
        c.StreamingMode = update.StreamingMode;
        c.WatermarkId = update.WatermarkId;
        c.FallbackFillerId = update.FallbackFillerId;
        await dbContext.SaveChangesAsync();

        searchTargets.SearchTargetsChanged();

        if (c.SubtitleMode != ChannelSubtitleMode.None)
        {
            Option<Playout> maybePlayout = await dbContext.Playouts
                .SelectOneAsync(p => p.ChannelId, p => p.ChannelId == c.Id);

            foreach (Playout playout in maybePlayout)
            {
                await workerChannel.WriteAsync(new ExtractEmbeddedSubtitles(playout.Id));
            }
        }

        await workerChannel.WriteAsync(new RefreshChannelList());

        return ProjectToViewModel(c);
    }

    private static async Task<Validation<BaseError, Channel>> Validate(TvContext dbContext, UpdateChannel request) =>
        (await ChannelMustExist(dbContext, request), ValidateName(request),
            await ValidateNumber(dbContext, request))
        .Apply((channelToUpdate, _, _) => channelToUpdate);

    private static Task<Validation<BaseError, Channel>> ChannelMustExist(
        TvContext dbContext,
        UpdateChannel updateChannel) =>
        dbContext.Channels
            .Include(c => c.Artwork)
            .Include(c => c.Watermark)
            .SelectOneAsync(c => c.Id, c => c.Id == updateChannel.ChannelId)
            .Map(o => o.ToValidation<BaseError>("Channel does not exist."));

    private static Validation<BaseError, string> ValidateName(UpdateChannel updateChannel) =>
        updateChannel.NotEmpty(c => c.Name)
            .Bind(_ => updateChannel.NotLongerThan(50)(c => c.Name));

    private static async Task<Validation<BaseError, string>> ValidateNumber(
        TvContext dbContext,
        UpdateChannel updateChannel)
    {
        int matchId = await dbContext.Channels
            .SelectOneAsync(c => c.Number, c => c.Number == updateChannel.Number)
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
