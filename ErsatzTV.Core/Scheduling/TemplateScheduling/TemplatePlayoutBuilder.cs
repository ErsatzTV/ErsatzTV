using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ErsatzTV.Core.Scheduling.TemplateScheduling;

public class TemplatePlayoutBuilder(
    ILocalFileSystem localFileSystem,
    IConfigElementRepository configElementRepository,
    IMediaCollectionRepository mediaCollectionRepository,
    ILogger<TemplatePlayoutBuilder> logger)
    : ITemplatePlayoutBuilder
{
    public async Task<Playout> Build(Playout playout, PlayoutBuildMode mode, CancellationToken cancellationToken)
    {
        if (!localFileSystem.FileExists(playout.TemplateFile))
        {
            logger.LogWarning("Playout template file {File} does not exist; aborting.", playout.TemplateFile);
            return playout;
        }

        PlayoutTemplate playoutTemplate = await LoadTemplate(playout, cancellationToken);

        DateTimeOffset start = DateTimeOffset.Now;
        int daysToBuild = await GetDaysToBuild();
        DateTimeOffset finish = start.AddDays(daysToBuild);

        if (mode == PlayoutBuildMode.Reset)
        {
            playout.Items.Clear();
        }

        DateTimeOffset currentTime = start;

        // load content and content enumerators on demand
        Dictionary<string, IMediaCollectionEnumerator> enumerators = new();

        var index = 0;
        while (currentTime < finish)
        {
            if (index >= playoutTemplate.Playout.Count)
            {
                logger.LogInformation("Reached the end of the playout template; stopping");
                break;
            }

            PlayoutTemplateItem playoutItem = playoutTemplate.Playout[index];

            // repeat resets index into template playout
            if (playoutItem is PlayoutTemplateRepeatItem)
            {
                index = 0;
                continue;
            }

            if (!enumerators.TryGetValue(playoutItem.Content, out IMediaCollectionEnumerator enumerator))
            {
                Option<IMediaCollectionEnumerator> maybeEnumerator =
                    await GetEnumeratorForContent(playoutItem.Content, playoutTemplate, cancellationToken);

                if (maybeEnumerator.IsNone)
                {
                    logger.LogWarning("Unable to locate content with key {Key}", playoutItem.Content);
                    continue;
                }

                foreach (IMediaCollectionEnumerator e in maybeEnumerator)
                {
                    enumerator = maybeEnumerator.ValueUnsafe();
                    enumerators.Add(playoutItem.Content, enumerator);
                }
            }

            switch (playoutItem)
            {
                case PlayoutTemplateCountItem count:
                    currentTime = PlayoutTemplateSchedulerCount.Schedule(playout, currentTime, count, enumerator);
                    break;
                case PlayoutTemplatePadToNextItem padToNext:
                    break;
            }

            index++;
        }

        return playout;
    }

    private async Task<int> GetDaysToBuild() =>
        await configElementRepository
            .GetValue<int>(ConfigElementKey.PlayoutDaysToBuild)
            .IfNoneAsync(2);

    private async Task<Option<IMediaCollectionEnumerator>> GetEnumeratorForContent(
        string contentKey,
        PlayoutTemplate playoutTemplate,
        CancellationToken cancellationToken)
    {
        Option<PlayoutTemplateContentSearchItem> maybeContent =
            playoutTemplate.Content.Where(c => c.Key == contentKey).HeadOrNone();

        if (maybeContent.IsNone)
        {
            return Option<IMediaCollectionEnumerator>.None;
        }

        foreach (PlayoutTemplateContentSearchItem content in maybeContent)
        {
            List<MediaItem> items = await mediaCollectionRepository.GetSmartCollectionItems(content.Query);
            var state = new CollectionEnumeratorState { Seed = 0, Index = 0 };
            switch (Enum.Parse<PlaybackOrder>(content.Order, true))
            {
                case PlaybackOrder.Chronological:
                    return new ChronologicalMediaCollectionEnumerator(items, state);
                case PlaybackOrder.Shuffle:
                    // TODO: fix this
                    var groupedMediaItems = items.Map(mi => new GroupedMediaItem(mi, null)).ToList();
                    return new ShuffledMediaCollectionEnumerator(groupedMediaItems, state, cancellationToken);
            }
        }

        return Option<IMediaCollectionEnumerator>.None;
    }

    private static async Task<PlayoutTemplate> LoadTemplate(Playout playout, CancellationToken cancellationToken)
    {
        string yaml = await File.ReadAllTextAsync(playout.TemplateFile, cancellationToken);

        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeDiscriminatingNodeDeserializer(
                o =>
                {
                    var keyMappings = new Dictionary<string, Type>
                    {
                        { "count", typeof(PlayoutTemplateCountItem) },
                        { "pad_to_next", typeof(PlayoutTemplatePadToNextItem) },
                        { "repeat", typeof(PlayoutTemplateRepeatItem) }
                    };

                    o.AddUniqueKeyTypeDiscriminator<PlayoutTemplateItem>(keyMappings);
                })
            .Build();

        return deserializer.Deserialize<PlayoutTemplate>(yaml);
    }


}
