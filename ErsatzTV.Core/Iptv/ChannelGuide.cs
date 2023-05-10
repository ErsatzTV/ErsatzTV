using System.Text;
using System.Xml;
using Microsoft.IO;

namespace ErsatzTV.Core.Iptv;

public class ChannelGuide
{
    private readonly Dictionary<string, string> _channelDataFragments;
    private readonly string _channelsFragment;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

    public ChannelGuide(
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        string channelsFragment,
        Dictionary<string, string> channelDataFragments)
    {
        _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        _channelsFragment = channelsFragment;
        _channelDataFragments = channelDataFragments;
    }

    public string ToXml()
    {
        using MemoryStream ms = _recyclableMemoryStreamManager.GetStream();
        using var xml = XmlWriter.Create(ms);
        xml.WriteStartDocument();

        xml.WriteStartElement("tv");
        xml.WriteAttributeString("generator-info-name", "ersatztv");

        xml.WriteRaw(_channelsFragment);

        foreach ((string channelNumber, string channelDataFragment) in _channelDataFragments.OrderBy(
                     kvp => decimal.Parse(kvp.Key)))
        {
            xml.WriteRaw(channelDataFragment);
        }

        xml.WriteEndElement(); // tv
        xml.WriteEndDocument();

        xml.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }
}
