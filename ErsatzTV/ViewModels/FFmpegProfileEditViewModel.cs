using ErsatzTV.Application.FFmpegProfiles;
using ErsatzTV.Application.FFmpegProfiles.Commands;
using ErsatzTV.Application.Resolutions;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.ViewModels
{
    public class FFmpegProfileEditViewModel
    {
        public FFmpegProfileEditViewModel()
        {
        }

        public FFmpegProfileEditViewModel(FFmpegProfileViewModel viewModel)
        {
            AudioBitrate = viewModel.AudioBitrate;
            AudioBufferSize = viewModel.AudioBufferSize;
            AudioChannels = viewModel.AudioChannels;
            AudioCodec = viewModel.AudioCodec;
            AudioSampleRate = viewModel.AudioSampleRate;
            AudioVolume = viewModel.AudioVolume;
            Id = viewModel.Id;
            Name = viewModel.Name;
            NormalizeAudio = viewModel.NormalizeAudio;
            NormalizeAudioCodec = viewModel.NormalizeAudioCodec;
            NormalizeResolution = viewModel.NormalizeResolution;
            NormalizeVideoCodec = viewModel.NormalizeVideoCodec;
            Resolution = viewModel.Resolution;
            ThreadCount = viewModel.ThreadCount;
            Transcode = viewModel.Transcode;
            HardwareAcceleration = viewModel.HardwareAcceleration;
            VideoBitrate = viewModel.VideoBitrate;
            VideoBufferSize = viewModel.VideoBufferSize;
            VideoCodec = viewModel.VideoCodec;
        }

        public int AudioBitrate { get; set; }
        public int AudioBufferSize { get; set; }
        public int AudioChannels { get; set; }
        public string AudioCodec { get; set; }
        public int AudioSampleRate { get; set; }
        public int AudioVolume { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public bool NormalizeAudio { get; set; }
        public bool NormalizeAudioCodec { get; set; }
        public bool NormalizeResolution { get; set; }
        public bool NormalizeVideoCodec { get; set; }
        public ResolutionViewModel Resolution { get; set; }
        public int ThreadCount { get; set; }
        public bool Transcode { get; set; }
        public HardwareAccelerationKind HardwareAcceleration { get; set; }
        public int VideoBitrate { get; set; }
        public int VideoBufferSize { get; set; }
        public string VideoCodec { get; set; }

        public CreateFFmpegProfile ToCreate() =>
            new(
                Name,
                ThreadCount,
                Transcode,
                HardwareAcceleration,
                Resolution.Id,
                NormalizeResolution,
                VideoCodec,
                NormalizeAudioCodec,
                VideoBitrate,
                VideoBufferSize,
                AudioCodec,
                NormalizeAudioCodec,
                AudioBitrate,
                AudioBufferSize,
                AudioVolume,
                AudioChannels,
                AudioSampleRate,
                NormalizeAudio
            );

        public UpdateFFmpegProfile ToUpdate() =>
            new(
                Id,
                Name,
                ThreadCount,
                Transcode,
                HardwareAcceleration,
                Resolution.Id,
                NormalizeResolution,
                VideoCodec,
                NormalizeAudioCodec,
                VideoBitrate,
                VideoBufferSize,
                AudioCodec,
                NormalizeAudioCodec,
                AudioBitrate,
                AudioBufferSize,
                AudioVolume,
                AudioChannels,
                AudioSampleRate,
                NormalizeAudio
            );
    }
}
