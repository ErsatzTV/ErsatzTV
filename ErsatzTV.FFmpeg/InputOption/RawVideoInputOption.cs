using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.InputOption;

public class RawVideoInputOption(string pixelFormat, FrameSize frameSize, int frameRate) : IInputOption
{
    public EnvironmentVariable[] EnvironmentVariables => [];
    public string[] GlobalOptions => [];

    public string[] InputOptions(InputFile inputFile) =>
    [
        "-f", "rawvideo",
        "-vcodec", "rawvideo",
        "-pix_fmt", pixelFormat,
        "-s", $"{frameSize.Width}x{frameSize.Height}",
        "-r", $"{frameRate}"
    ];

    public string[] FilterOptions => [];
    public string[] OutputOptions => [];
    public FrameState NextState(FrameState currentState) => currentState;

    public bool AppliesTo(AudioInputFile audioInputFile) => false;

    public bool AppliesTo(VideoInputFile videoInputFile) => false;

    public bool AppliesTo(ConcatInputFile concatInputFile) => false;

    public bool AppliesTo(GraphicsEngineInput graphicsEngineInput) => true;
}
