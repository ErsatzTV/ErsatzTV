using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.IO;

namespace ErsatzTV.Scanner.Core.Metadata.Nfo;

public abstract class NfoReader<T>
{
    private static readonly byte[] Buffer = new byte[8 * 1024 * 1024];
    private static readonly Regex Pattern = new(@"[\p{C}-[\r\n\t]]+");

    protected static readonly XmlReaderSettings Settings =
        new()
        {
            Async = true,
            ConformanceLevel = ConformanceLevel.Fragment,
            ValidationType = ValidationType.None,
            CheckCharacters = false,
            IgnoreProcessingInstructions = true,
            IgnoreComments = true
        };

    private readonly ILogger _logger;

    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

    protected NfoReader(RecyclableMemoryStreamManager recyclableMemoryStreamManager, ILogger logger)
    {
        _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        _logger = logger;
    }

    protected async Task<Stream> SanitizedStreamForFile(string fileName)
    {
        await using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, Buffer.Length, true);
        using var sr = new StreamReader(fs, Encoding.UTF8);
        string text = await sr.ReadToEndAsync();
        // trim BOM and zero width space, replace controls with replacement character
        string stripped = Pattern.Replace(text.Trim('\uFEFF', '\u200B'), "\ufffd");

        MemoryStream ms = _recyclableMemoryStreamManager.GetStream();
        await ms.WriteAsync(Encoding.UTF8.GetBytes(stripped));
        ms.Position = 0;
        return ms;
    }

    protected async Task ReadStringContent(XmlReader reader, T nfo, Action<T, string> action)
    {
        try
        {
            if (nfo != null)
            {
                string result = await reader.ReadElementContentAsStringAsync();
                action(nfo, result);
            }
        }
        catch (XmlException ex)
        {
            _logger.LogWarning(ex, "Error reading string content from NFO {ElementName}", reader.Name);
        }
    }

    protected async Task ReadIntContent(XmlReader reader, T nfo, Action<T, int> action)
    {
        try
        {
            if (nfo != null && int.TryParse(await reader.ReadElementContentAsStringAsync(), out int result))
            {
                action(nfo, result);
            }
        }
        catch (XmlException ex)
        {
            _logger.LogWarning(ex, "Error reading int content from NFO {ElementName}", reader.Name);
        }
    }

    protected async Task ReadDateTimeContent(XmlReader reader, T nfo, Action<T, DateTime> action)
    {
        try
        {
            if (nfo != null && DateTime.TryParse(await reader.ReadElementContentAsStringAsync(), out DateTime result))
            {
                action(nfo, result);
            }
        }
        catch (XmlException ex)
        {
            _logger.LogWarning(ex, "Error reading date content from NFO {ElementName}", reader.Name);
        }
    }

    protected void ReadActor(XmlReader reader, T nfo, Action<T, ActorNfo> action)
    {
        try
        {
            if (nfo != null)
            {
                var actor = new ActorNfo();
                var element = (XElement)XNode.ReadFrom(reader);

                XElement name = element.Element("name");
                if (name != null)
                {
                    actor.Name = name.Value;
                }

                XElement role = element.Element("role");
                if (role != null)
                {
                    actor.Role = role.Value;
                }

                XElement order = element.Element("order");
                if (order != null && int.TryParse(order.Value, out int orderValue))
                {
                    actor.Order = orderValue;
                }

                XElement thumb = element.Element("thumb");
                if (thumb != null)
                {
                    actor.Thumb = thumb.Value;
                }

                action(nfo, actor);
            }
        }
        catch (XmlException ex)
        {
            _logger.LogWarning(ex, "Error reading actor content from NFO {ElementName}", reader.Name);
        }
    }

    protected async Task ReadUniqueId(XmlReader reader, T nfo, Action<T, UniqueIdNfo> action)
    {
        try
        {
            if (nfo != null)
            {
                var uniqueId = new UniqueIdNfo();
                reader.MoveToAttribute("default");
                uniqueId.Default = bool.TryParse(reader.Value, out bool def) && def;
                reader.MoveToAttribute("type");
                uniqueId.Type = reader.Value;
                reader.MoveToElement();
                uniqueId.Guid = await reader.ReadElementContentAsStringAsync();

                action(nfo, uniqueId);
            }
        }
        catch (XmlException ex)
        {
            _logger.LogWarning(ex, "Error reading uniqueid content from NFO {ElementName}", reader.Name);
        }
    }
}
