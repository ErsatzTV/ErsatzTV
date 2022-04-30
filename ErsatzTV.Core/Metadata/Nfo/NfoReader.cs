using System.Xml;
using System.Xml.Linq;

namespace ErsatzTV.Core.Metadata.Nfo;

public abstract class NfoReader<T>
{
    protected static async Task ReadStringContent(XmlReader reader, T nfo, Action<T, string> action)
    {
        if (nfo != null)
        {
            string result = await reader.ReadElementContentAsStringAsync();
            action(nfo, result);
        }
    }

    protected static async Task ReadIntContent(XmlReader reader, T nfo, Action<T, int> action)
    {
        if (nfo != null && int.TryParse(await reader.ReadElementContentAsStringAsync(), out int result))
        {
            action(nfo, result);
        }
    }

    protected static async Task ReadDateTimeContent(XmlReader reader, T nfo, Action<T, DateTime> action)
    {
        if (nfo != null && DateTime.TryParse(await reader.ReadElementContentAsStringAsync(), out DateTime result))
        {
            action(nfo, result);
        }
    }

    protected static void ReadActor(XmlReader reader, T nfo, Action<T, ActorNfo> action)
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

    protected static async Task ReadUniqueId(XmlReader reader, T nfo, Action<T, UniqueIdNfo> action)
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
}
