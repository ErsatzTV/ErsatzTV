using System.Collections.Generic;
using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Plex.Models;

public class PlexMediaContainerResponse<T>
{
    public T MediaContainer { get; set; }
}

public class PlexMediaContainerDirectoryContent<T>
{
    public List<T> Directory { get; set; }
}

public class PlexMediaContainerMetadataContent<T>
{
    public List<T> Metadata { get; set; }
}

[XmlRoot("MediaContainer", Namespace = null)]
public class PlexXmlVideoMetadataResponseContainer
{
    [XmlElement("Video")]
    public PlexXmlMetadataResponse Metadata { get; set; }
}

[XmlRoot("MediaContainer", Namespace = null)]
public class PlexXmlDirectoryMetadataResponseContainer
{
    [XmlElement("Directory")]
    public PlexXmlMetadataResponse Metadata { get; set; }
}

[XmlRoot("MediaContainer", Namespace = null)]
public class PlexXmlSeasonsMetadataResponseContainer
{
    [XmlElement("Directory")]
    public List<PlexXmlMetadataResponse> Metadata { get; set; }
}

[XmlRoot("MediaContainer", Namespace = null)]
public class PlexXmlEpisodesMetadataResponseContainer
{
    [XmlElement("Video")]
    public List<PlexXmlMetadataResponse> Metadata { get; set; }
}