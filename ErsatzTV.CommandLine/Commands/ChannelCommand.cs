using System;
using System.Linq;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using ErsatzTV.Api.Sdk.Api;
using ErsatzTV.Api.Sdk.Model;
using LanguageExt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.CommandLine.Commands
{
    [Command("channel", Description = "Create or rename a channel")]
    public class ChannelCommand : ICommand
    {
        private readonly ChannelsApi _channelsApi;
        private readonly FFmpegProfileApi _ffmpegProfileApi;
        private readonly ILogger<ChannelCommand> _logger;

        public ChannelCommand(IConfiguration configuration, ILogger<ChannelCommand> logger)
        {
            _logger = logger;
            _channelsApi = new ChannelsApi(configuration["ServerUrl"]);
            _ffmpegProfileApi = new FFmpegProfileApi(configuration["ServerUrl"]);
        }

        [CommandParameter(0, Name = "channel-number", Description = "The channel number")]
        public int Number { get; set; }

        [CommandParameter(1, Name = "channel-name", Description = "The channel name")]
        public string Name { get; set; }

        [CommandParameter(2, Name = "streaming-mode", Description = "The streaming mode")]
        public StreamingMode StreamingMode { get; set; }

        [CommandOption("ffmpeg-profile", Description = "The ffmpeg profile name")]
        public string FFmpegProfileName { get; set; }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            try
            {
                Option<ChannelViewModel> maybeChannel = await _channelsApi.ApiChannelsGetAsync()
                    .Map(list => Optional(list.SingleOrDefault(c => c.Number == Number)));

                FFmpegProfileViewModel ffmpegProfile = await _ffmpegProfileApi.ApiFfmpegProfilesGetAsync()
                    .Map(
                        list => Optional(list.SingleOrDefault(p => p.Name == FFmpegProfileName))
                            .IfNone(new FFmpegProfileViewModel { Id = 1 }));

                await maybeChannel.Match(
                    channel => RenameChannel(channel, ffmpegProfile),
                    () => AddChannel(ffmpegProfile));
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to synchronize channel: {Message}", ex.Message);
            }
        }

        private async ValueTask RenameChannel(ChannelViewModel existing, FFmpegProfileViewModel ffmpegProfile)
        {
            int newFFmpegProfileId = string.IsNullOrWhiteSpace(FFmpegProfileName)
                ? existing.FfmpegProfileId
                : ffmpegProfile.Id;

            if (existing.Name != Name || existing.FfmpegProfileId != newFFmpegProfileId ||
                existing.StreamingMode != StreamingMode)
            {
                var updateChannel = new UpdateChannel(
                    existing.Id,
                    Name,
                    existing.Number,
                    newFFmpegProfileId,
                    existing.Logo,
                    StreamingMode);

                await _channelsApi.ApiChannelsPatchAsync(updateChannel);
            }

            _logger.LogInformation(
                "Successfully synchronized channel {ChannelNumber} - {ChannelName}",
                Number,
                Name);
        }

        private async ValueTask AddChannel(FFmpegProfileViewModel ffmpegProfile)
        {
            var createChannel = new CreateChannel(
                Name,
                Number,
                ffmpegProfile.Id,
                null,
                StreamingMode);

            await _channelsApi.ApiChannelsPostAsync(createChannel);

            _logger.LogInformation(
                "Successfully created channel {ChannelNumber} - {ChannelName}",
                Number,
                Name);
        }
    }
}
