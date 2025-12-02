namespace ErsatzTV.FFmpeg;

public record FrameRate(string? FrameRateString)
{
    public double ParsedFrameRate { get; init; } = ParseFrameRate(FrameRateString, 24.0);

    public string RFrameRate { get; init; } = FrameRateString ?? "24";

    public static FrameRate DefaultFrameRate => new("24");

    public static double ParseFrameRate(string? rFrameRate, double defaultFrameRate)
    {
        double frameRate = defaultFrameRate;

        if (double.TryParse(rFrameRate, out double value))
        {
            frameRate = value;
        }
        else
        {
            string[] split = (rFrameRate ?? string.Empty).Split("/");
            if (int.TryParse(split[0], out int left) && int.TryParse(split[1], out int right) && right != 0)
            {
                frameRate = left / (double)right;
            }
        }

        return frameRate;
    }
}
