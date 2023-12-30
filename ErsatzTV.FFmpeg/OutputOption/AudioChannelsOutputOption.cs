using System.Globalization;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.OutputOption;

public class AudioChannelsOutputOption : OutputOption
{
    private readonly Option<string> _audioFormat;
    private readonly int _desiredChannels;
    private readonly int _sourceChannels;

    public AudioChannelsOutputOption(Option<string> audioFormat, int sourceChannels, int desiredChannels)
    {
        _audioFormat = audioFormat;
        _sourceChannels = sourceChannels;
        _desiredChannels = desiredChannels;
    }

    public override string[] OutputOptions
    {
        get
        {
            if (_sourceChannels != _desiredChannels || _audioFormat == Some(AudioFormat.Aac) && _desiredChannels > 2)
            {
                return new[]
                {
                    "-ac", _desiredChannels.ToString(CultureInfo.InvariantCulture)
                };
            }

            return Array.Empty<string>();
        }
    }
}
