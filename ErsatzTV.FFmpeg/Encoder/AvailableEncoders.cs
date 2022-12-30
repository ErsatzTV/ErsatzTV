using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Encoder;

public static class AvailableEncoders
{
    public static Option<IEncoder> ForAudioFormat(AudioState desiredState, ILogger logger) =>
        desiredState.AudioFormat.Match(
            audioFormat =>
                audioFormat switch
                {
                    AudioFormat.Aac => (Option<IEncoder>)new EncoderAac(),
                    AudioFormat.Ac3 => new EncoderAc3(),
                    AudioFormat.Copy => new EncoderCopyAudio(),
                    _ => LogUnknownEncoder(audioFormat, logger)
                },
            () => LogUnknownEncoder(string.Empty, logger));

    private static Option<IEncoder> LogUnknownEncoder(
        string audioFormat,
        ILogger logger)
    {
        logger.LogWarning("Unable to determine audio encoder for {AudioFormat}; may have playback issues", audioFormat);
        return Option<IEncoder>.None;
    }
}
