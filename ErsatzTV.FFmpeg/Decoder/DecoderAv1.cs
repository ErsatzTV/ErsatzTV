namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderAv1 : DecoderBase
{
    // ReSharper disable IdentifierTypo
    private const string Libdav1d = "libdav1d";
    private const string Libaomav1 = "libaom-av1";
    
    private readonly IReadOnlySet<string> _ffmpegDecoders;

    public DecoderAv1(IReadOnlySet<string> ffmpegDecoders)
    {
        _ffmpegDecoders = ffmpegDecoders;
    }
    
    public override string Name
    {
        get
        {
            if (_ffmpegDecoders.Contains(Libdav1d))
            {
                return Libdav1d;
            }

            return _ffmpegDecoders.Contains(Libaomav1) ? Libaomav1 : "av1";
        }
    }

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
}
