using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.OutputOption;

public class AmdCropMetadataWorkaroundFilter(string videoFormat, FrameSize frameSize) : OutputOption
{
    public override string[] OutputOptions
    {
        get
        {
            if (videoFormat is not VideoFormat.Hevc)
            {
                return [];
            }

            int cropPixels = frameSize.Height switch
            {
                1080 => 8,
                _ => 0
            };

            if (cropPixels == 0)
            {
                return [];
            }

            return
            [
                "-bsf:v",
                $"{videoFormat}_metadata=crop_bottom={cropPixels}"
            ];
        }
    }
}
