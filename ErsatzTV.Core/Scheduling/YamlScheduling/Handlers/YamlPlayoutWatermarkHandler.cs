using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutWatermarkHandler(IChannelRepository channelRepository) : IYamlPlayoutHandler
{
    public bool Reset => false;

    public async Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        PlayoutBuildMode mode,
        ILogger<YamlPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (instruction is not YamlPlayoutWatermarkInstruction watermark)
        {
            return false;
        }

        if (watermark.Watermark && !string.IsNullOrWhiteSpace(watermark.Name))
        {
            Option<ChannelWatermark> maybeWatermark = await channelRepository.GetWatermarkByName(watermark.Name);
            foreach (ChannelWatermark channelWatermark in maybeWatermark)
            {
                context.SetChannelWatermarkId(channelWatermark.Id);
            }
        }
        else
        {
            context.ClearChannelWatermarkId();
        }

        return true;
    }
}
