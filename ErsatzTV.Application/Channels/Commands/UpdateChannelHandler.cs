using System.Globalization;
using System.Text.RegularExpressions;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Channels.Mapper;

namespace ErsatzTV.Application.Channels;

public class UpdateChannelHandler : IRequestHandler<UpdateChannel, Either<BaseError, ChannelViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public UpdateChannelHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Either<BaseError, ChannelViewModel>> Handle(
        UpdateChannel request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Channel> validation = await Validate(dbContext, request);
        return await LanguageExtensions.Apply(validation, c => ApplyUpdateRequest(dbContext, c, request));
    }

    private async Task<ChannelViewModel> ApplyUpdateRequest(TvContext dbContext, Channel c, UpdateChannel update)
    {
        c.Name = update.Name;
        c.Number = update.Number;
        c.Group = update.Group;
        c.Categories = update.Categories;
        c.FFmpegProfileId = update.FFmpegProfileId;
        c.PreferredAudioLanguageCode = update.PreferredAudioLanguageCode;
        c.PreferredSubtitleLanguageCode = update.PreferredSubtitleLanguageCode;
        c.SubtitleMode = update.SubtitleMode;
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
            
        c.StreamingMode = update.StreamingMode;
        c.WatermarkId = update.WatermarkId;
        c.FallbackFillerId = update.FallbackFillerId;
        await dbContext.SaveChangesAsync();
        return ProjectToViewModel(c);
    }

    private async Task<Validation<BaseError, Channel>> Validate(TvContext dbContext, UpdateChannel request) =>
        (await ChannelMustExist(dbContext, request), ValidateName(request),
            await ValidateNumber(dbContext, request),
            ValidatePreferredAudioLanguage(request))
        .Apply((channelToUpdate, _, _, _) => channelToUpdate);

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

            return BaseError.New("Invalid channel number; one decimal is allowed for subchannels");
        }

        return BaseError.New("Channel number must be unique");
    }

    private static Validation<BaseError, string> ValidatePreferredAudioLanguage(UpdateChannel updateChannel) =>
        Optional(updateChannel.PreferredAudioLanguageCode ?? string.Empty)
            .Filter(
                lc => string.IsNullOrWhiteSpace(lc) || CultureInfo.GetCultures(CultureTypes.NeutralCultures).Any(
                    ci => string.Equals(ci.ThreeLetterISOLanguageName, lc, StringComparison.OrdinalIgnoreCase)))
            .ToValidation<BaseError>("Preferred audio language code is invalid");
}