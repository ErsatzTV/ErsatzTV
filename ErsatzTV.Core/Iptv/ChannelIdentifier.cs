using System.Globalization;

namespace ErsatzTV.Core.Iptv;

public static class ChannelIdentifier
{
    public static string LegacyFromNumber(string channelNumber) => $"{channelNumber}.etv";

    public static string FromNumber(string channelNumber)
    {
        // get rid of any decimal (only two are allowed)
        var number = (int)(decimal.Parse(channelNumber, CultureInfo.InvariantCulture) * 100);
        var id = 0;
        while (number != 0)
        {
            id += number % 10 + 48;
            number /= 10;
        }

        string instanceId = SystemEnvironment.InstanceId;
        return !string.IsNullOrWhiteSpace(instanceId)
            ? $"C{channelNumber}.{id}.{instanceId}.ersatztv.org"
            : $"C{channelNumber}.{id}.ersatztv.org";
    }
}
