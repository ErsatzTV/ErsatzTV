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
            NormalizeLoudness = viewModel.NormalizeLoudness;
            Id = viewModel.Id;
            Name = viewModel.Name;
            NormalizeAudio = viewModel.NormalizeAudio;
            NormalizeVideo = viewModel.NormalizeVideo;
            Resolution = viewModel.Resolution;
            ThreadCount = viewModel.ThreadCount;
            Transcode = viewModel.Transcode;
            HardwareAcceleration = viewModel.HardwareAcceleration;
            VideoBitrate = viewModel.VideoBitrate;
            VideoBufferSize = viewModel.VideoBufferSize;
            VideoCodec = viewModel.VideoCodec;
            FrameRate = viewModel.FrameRate;
        }

        public int AudioBitrate { get; set; }
        public int AudioBufferSize { get; set; }
        public int AudioChannels { get; set; }
        public string AudioCodec { get; set; }
        public int AudioSampleRate { get; set; }
        public bool NormalizeLoudness { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public bool NormalizeAudio { get; set; }
        public bool NormalizeVideo { get; set; }
        public ResolutionViewModel Resolution { get; set; }
        public int ThreadCount { get; set; }
        public bool Transcode { get; set; }
        public HardwareAccelerationKind HardwareAcceleration { get; set; }
        public int VideoBitrate { get; set; }
        public int VideoBufferSize { get; set; }
        public string VideoCodec { get; set; }
        public string FrameRate { get; set; }

        public CreateFFmpegProfile ToCreate() =>
            new(
                Name,
                ThreadCount,
                Transcode,
                HardwareAcceleration,
                Resolution.Id,
                NormalizeVideo,
                VideoCodec,
                VideoBitrate,
                VideoBufferSize,
                AudioCodec,
                AudioBitrate,
                AudioBufferSize,
                NormalizeLoudness,
                AudioChannels,
                AudioSampleRate,
                NormalizeAudio,
                FrameRate
            );

        public UpdateFFmpegProfile ToUpdate() =>
            new(
                Id,
                Name,
                ThreadCount,
                Transcode,
                HardwareAcceleration,
                Resolution.Id,
                NormalizeVideo,
                VideoCodec,
                VideoBitrate,
                VideoBufferSize,
                AudioCodec,
                AudioBitrate,
                AudioBufferSize,
                NormalizeLoudness,
                AudioChannels,
                AudioSampleRate,
                NormalizeAudio,
                FrameRate
            );
    }
}
