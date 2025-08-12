using System.Globalization;
using System.Xml;
using System.Xml.Serialization;
using ErsatzTV.Infrastructure.Epg.Models;

namespace ErsatzTV.Infrastructure.Epg;

public static class EpgReader
{
    private const string XmlTvDateFormat = "yyyyMMddHHmmss zzz";

    public static Option<EpgProgramme> FindProgrammeAt(Stream xmlStream, DateTimeOffset targetTime)
    {
        var serializer = new XmlSerializer(typeof(EpgProgramme));

        var settings = new XmlReaderSettings
        {
            ConformanceLevel = ConformanceLevel.Fragment
        };

        using var reader = XmlReader.Create(xmlStream, settings);

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.Name != "programme")
            {
                continue;
            }

            string startStr = reader.GetAttribute("start");
            string stopStr = reader.GetAttribute("stop");

            if (startStr == null || stopStr == null)
            {
                continue;
            }

            if (DateTimeOffset.TryParseExact(startStr, XmlTvDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) &&
                DateTimeOffset.TryParseExact(stopStr, XmlTvDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var stop))
            {
                if (start <= targetTime && targetTime < stop)
                {
                    using var subtreeReader = reader.ReadSubtree();
                    return Optional(serializer.Deserialize(subtreeReader) as EpgProgramme);
                }
            }
        }

        return Option<EpgProgramme>.None;
    }
}
