using LanguageExt;

namespace ErsatzTV.Core.Domain
{
    public abstract class MediaCollection : Record<MediaCollection>
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
