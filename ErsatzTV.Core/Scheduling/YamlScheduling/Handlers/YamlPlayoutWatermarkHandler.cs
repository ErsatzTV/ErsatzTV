using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutWatermarkHandler(IChannelRepository channelRepository) : IYamlPlayoutHandler
{
    private readonly Dictionary<string, Option<ChannelWatermark>> _watermarkCache = new();

    public bool Reset => false;

    public async Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        PlayoutBuildMode mode,
        Func<string, Task> executeSequence,
        ILogger<YamlPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (instruction is not YamlPlayoutWatermarkInstruction watermark)
        {
            return false;
        }

        if (watermark.Watermark && !string.IsNullOrWhiteSpace(watermark.Name))
        {
            foreach (var wm in await GetChannelWatermarkByName(watermark.Name))
            {
                context.SetChannelWatermarkId(wm.Id);
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(watermark.Name))
            {
                foreach (var wm in await GetChannelWatermarkByName(watermark.Name))
                {
                    context.RemoveChannelWatermarkId(wm.Id);
                }
            }
            else
            {
                context.ClearChannelWatermarkIds();
            }
        }

        return true;
    }

    private async Task<Option<ChannelWatermark>> GetChannelWatermarkByName(string name)
    {
        if (_watermarkCache.TryGetValue(name, out var cachedWatermark))
        {
            foreach (ChannelWatermark channelWatermark in cachedWatermark)
            {
                return channelWatermark;
            }
        }
        else
        {
            Option<ChannelWatermark> maybeWatermark = await channelRepository.GetWatermarkByName(name);
            _watermarkCache.Add(name, maybeWatermark);
            foreach (ChannelWatermark channelWatermark in maybeWatermark)
            {
                return channelWatermark;
            }
        }

        return Option<ChannelWatermark>.None;
    }
}
