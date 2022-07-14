using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Option;

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

    public override IList<string> OutputOptions
    {
        get
        {
            if (_sourceChannels != _desiredChannels || _audioFormat == Some(AudioFormat.Aac) && _desiredChannels > 2)
            {
                return new List<string>
                {
                    "-ac", _desiredChannels.ToString()
                };
            }

            return Array.Empty<string>();
        }
    }
}
