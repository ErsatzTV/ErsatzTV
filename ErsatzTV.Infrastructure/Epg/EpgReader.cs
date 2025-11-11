using System.Globalization;
using System.Xml;
using System.Xml.Serialization;
using ErsatzTV.Infrastructure.Epg.Models;

namespace ErsatzTV.Infrastructure.Epg;

public static class EpgReader
{
    public const string XmlTvDateFormat = "yyyyMMddHHmmss zzz";
    public const string XmlTvCustomNamespace = "https://ersatztv.org/xmltv/extensions";

    public static List<EpgProgramme> FindProgrammesAt(Stream xmlStream, DateTimeOffset targetTime, int count)
    {
        var result = new List<EpgProgramme>();

        var serializer = new XmlSerializer(typeof(EpgProgramme));
        var settings = new XmlReaderSettings
        {
            ConformanceLevel = ConformanceLevel.Fragment,
        };

        var nt = new NameTable();
        var nsmgr = new XmlNamespaceManager(nt);
        nsmgr.AddNamespace("etv", XmlTvCustomNamespace);
        var context = new XmlParserContext(nt, nsmgr, null, XmlSpace.None);

        using var reader = XmlReader.Create(xmlStream, settings, context);

        var foundCurrent = false;

        while (reader.Read() && count > 0)
        {
            if (reader.NodeType != XmlNodeType.Element || reader.Name != "programme")
            {
                continue;
            }

            if (foundCurrent)
            {
                using XmlReader subtreeReader = reader.ReadSubtree();
                Option<EpgProgramme> maybeSubtreeProgramme =
                    Optional(serializer.Deserialize(subtreeReader) as EpgProgramme);
                result.AddRange(maybeSubtreeProgramme);
                if (maybeSubtreeProgramme.IsNone)
                {
                    return result;
                }

                count--;
                continue;
            }

            string startStr = reader.GetAttribute("start");
            string stopStr = reader.GetAttribute("stop");

            if (startStr == null || stopStr == null)
            {
                continue;
            }

            if (DateTimeOffset.TryParseExact(
                    startStr,
                    XmlTvDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTimeOffset start) &&
                DateTimeOffset.TryParseExact(
                    stopStr,
                    XmlTvDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTimeOffset stop))
            {
                if (start <= targetTime && targetTime < stop)
                {
                    using XmlReader subtreeReader = reader.ReadSubtree();
                    Option<EpgProgramme> maybeCurrentProgramme =
                        Optional(serializer.Deserialize(subtreeReader) as EpgProgramme);
                    result.AddRange(maybeCurrentProgramme);
                    if (maybeCurrentProgramme.IsNone)
                    {
                        return result;
                    }

                    foundCurrent = true;
                    count--;
                }
            }
        }

        return result;
    }
}
