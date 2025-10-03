using System.Globalization;

namespace ErsatzTV.FFmpeg;

public static class AspectRatio
{
    public static string CalculateSAR(int width, int height, string displayAspectRatio)
    {
        // first check for decimal DAR
        if (!double.TryParse(displayAspectRatio, out double dar))
        {
            // if not, assume it's a ratio
            string[] split = displayAspectRatio.Split(':');
            var num = double.Parse(split[0], CultureInfo.InvariantCulture);
            var den = double.Parse(split[1], CultureInfo.InvariantCulture);
            dar = num / den;
        }

        double res = width / (double)height;
        var formattedDar = string.Format(
            CultureInfo.InvariantCulture,
            dar % 1 == 0 ? "{0:F0}" : "{0:0.############}",
            dar);
        var formattedRes = string.Format(
            CultureInfo.InvariantCulture,
            res % 1 == 0 ? "{0:F0}" : "{0:0.############}",
            res);
        return $"{formattedDar}:{formattedRes}";
    }
}
