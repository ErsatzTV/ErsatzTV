using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Qsv;

public class EncoderHevcQsv(Option<string> maybeVideoPreset) : EncoderBase
{
    public override string Name => "hevc_qsv";
    public override StreamKind Kind => StreamKind.Video;

    public override string[] OutputOptions
    {
        get
        {
            var result = new List<string> { "-c:v", Name, "-low_power", "0", "-look_ahead", "0" };

            foreach (string videoPreset in maybeVideoPreset)
            {
                if (!string.IsNullOrWhiteSpace(videoPreset))
                {
                    result.Add("-preset:v");
                    result.Add(videoPreset);
                }
            }

            return result.ToArray();
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Hevc,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
