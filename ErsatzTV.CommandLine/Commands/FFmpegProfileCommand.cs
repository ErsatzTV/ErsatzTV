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
    [Command("ffmpeg-profile", Description = "Synchronize an ffmpeg profile")]
    public class FFmpegProfileCommand : ICommand
    {
        private readonly FFmpegProfileApi _ffmpegProfileApi;
        private readonly ILogger<FFmpegProfileCommand> _logger;

        public FFmpegProfileCommand(IConfiguration configuration, ILogger<FFmpegProfileCommand> logger)
        {
            _logger = logger;
            _ffmpegProfileApi = new FFmpegProfileApi(configuration["ServerUrl"]);
        }

        [CommandParameter(0, Name = "profile-name", Description = "The ffmpeg profile name")]
        public string Name { get; set; }

        [CommandOption("thread-count", Description = "The number of threads")]
        public int ThreadCount { get; set; } = 4;

        [CommandOption("transcode", Description = "Whether to transcode all media")]
        public bool Transcode { get; set; } = true;

        // public int ResolutionId { get; set; } = resolution.Id;
        // Resolution { get; set; } = resolution;
        [CommandOption("resolution", Description = "The resolution")]
        public DesiredResolution Resolution { get; set; } = DesiredResolution.W1920H1080;

        [CommandOption("video-codec", Description = "The video codec")]
        public string VideoCodec { get; set; } = "libx264";

        [CommandOption("audio-codec", Description = "The audio codec")]
        public string AudioCodec { get; set; } = "ac3";

        [CommandOption("video-bitrate", Description = "The video bitrate in kBit/s")]
        public int VideoBitrate { get; set; } = 2000;

        [CommandOption("video-buffer-size", Description = "The video buffer size in kBit")]
        public int VideoBufferSize { get; set; } = 2000;

        [CommandOption("audio-bitrate", Description = "The audio bitrate in kBit/s")]
        public int AudioBitrate { get; set; } = 192;

        [CommandOption("audio-buffer-size", Description = "The audio buffer size in kBits")]
        public int AudioBufferSize { get; set; } = 50;

        [CommandOption("audio-volume", Description = "The audio volume as a whole number percent")]
        public int AudioVolume { get; set; } = 100;

        [CommandOption("audio-channels", Description = "The number of audio channels")]
        public int AudioChannels { get; set; } = 2;

        [CommandOption("audio-sample-rate", Description = "The audio sample rate in kHz")]
        public int AudioSampleRate { get; set; } = 48;

        [CommandOption("normalize-resolution", Description = "Whether to normalize the resolution of all media")]
        public bool NormalizeResolution { get; set; } = true;

        [CommandOption("normalize-video-codec", Description = "Whether to normalize the video codec of all media")]
        public bool NormalizeVideoCodec { get; set; } = true;

        [CommandOption("normalize-audio-codec", Description = "Whether to normalize the audio codec of all media")]
        public bool NormalizeAudioCodec { get; set; } = true;

        [CommandOption(
            "normalize-audio",
            Description = "Whether to normalize audio channels and sample rate of all media")]
        public bool NormalizeAudio { get; set; } = true;

        public async ValueTask ExecuteAsync(IConsole console)
        {
            try
            {
                Option<FFmpegProfileViewModel> maybeFFmpegProfile = await _ffmpegProfileApi.ApiFfmpegProfilesGetAsync()
                    .Map(list => Optional(list.SingleOrDefault(p => p.Name == Name)));

                await maybeFFmpegProfile.Match(UpdateProfile, AddProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to synchronize ffmpeg profile: {Message}", ex.Message);
            }
        }

        private async ValueTask UpdateProfile(FFmpegProfileViewModel existing)
        {
            var updateFFmpegProfile = new UpdateFFmpegProfile(
                existing.Id,
                Name,
                ThreadCount,
                Transcode,
                (int) Resolution,
                NormalizeResolution,
                VideoCodec,
                NormalizeVideoCodec,
                VideoBitrate,
                VideoBufferSize,
                AudioCodec,
                NormalizeAudioCodec,
                AudioBitrate,
                AudioBufferSize,
                AudioVolume,
                AudioChannels,
                AudioSampleRate,
                NormalizeAudio);

            await _ffmpegProfileApi.ApiFfmpegProfilesPatchAsync(updateFFmpegProfile);

            _logger.LogInformation("Successfully synchronized ffmpeg profile {ProfileName}", Name);
        }

        private async ValueTask AddProfile()
        {
            var createFFmpegProfile = new CreateFFmpegProfile(
                Name,
                ThreadCount,
                Transcode,
                (int) Resolution,
                NormalizeResolution,
                VideoCodec,
                NormalizeVideoCodec,
                VideoBitrate,
                VideoBufferSize,
                AudioCodec,
                NormalizeAudioCodec,
                AudioBitrate,
                AudioBufferSize,
                AudioVolume,
                AudioChannels,
                AudioSampleRate,
                NormalizeAudio);


            await _ffmpegProfileApi.ApiFfmpegProfilesPostAsync(createFFmpegProfile);

            _logger.LogInformation("Successfully created ffmpeg profile {ProfileName}", Name);
        }
    }
}
