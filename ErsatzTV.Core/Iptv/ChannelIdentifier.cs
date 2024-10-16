using System.Globalization;

namespace ErsatzTV.Core.Iptv;

public static class ChannelIdentifier
{
    public static string LegacyFromNumber(string channelNumber)
    {
        return $"{channelNumber}.etv";
    }

    public static string FromNumber(string channelNumber)
    {
        // get rid of any decimal (only two are allowed)
        int number = (int)(decimal.Parse(channelNumber, CultureInfo.InvariantCulture) * 100);
        int id = 0;
        while (number != 0)
        {
            id += number % 10 + 48;
            number /= 10;
        }

        return $"C{channelNumber}.{id}.ersatztv.org";
    }
}
