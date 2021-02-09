namespace ErsatzTV.Core.Domain
{
    public record FFmpegProfile
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ThreadCount { get; set; }
        public bool Transcode { get; set; }
        public int ResolutionId { get; set; }
        public Resolution Resolution { get; set; }
        public bool NormalizeResolution { get; set; }
        public string VideoCodec { get; set; }
        public bool NormalizeVideoCodec { get; set; }
        public int VideoBitrate { get; set; }
        public int VideoBufferSize { get; set; }
        public string AudioCodec { get; set; }
        public bool NormalizeAudioCodec { get; set; }
        public int AudioBitrate { get; set; }
        public int AudioBufferSize { get; set; }
        public int AudioVolume { get; set; }
        public int AudioChannels { get; set; }
        public int AudioSampleRate { get; set; }
        public bool NormalizeAudio { get; set; }

        public static FFmpegProfile New(string name, Resolution resolution) =>
            new()
            {
                Name = name,
                ThreadCount = 4,
                Transcode = true,
                ResolutionId = resolution.Id,
                Resolution = resolution,
                VideoCodec = "libx264",
                AudioCodec = "ac3",
                VideoBitrate = 2000,
                VideoBufferSize = 2000,
                AudioBitrate = 192,
                AudioBufferSize = 50,
                AudioVolume = 100,
                AudioChannels = 2,
                AudioSampleRate = 48,
                NormalizeResolution = true,
                NormalizeVideoCodec = true,
                NormalizeAudioCodec = true,
                NormalizeAudio = true
            };
    }
}
