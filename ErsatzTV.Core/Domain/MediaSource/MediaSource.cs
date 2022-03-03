using System.Collections.Generic;

namespace ErsatzTV.Core.Domain;

public abstract class MediaSource
{
    public int Id { get; set; }

    public List<Library> Libraries { get; set; }
}