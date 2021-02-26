namespace ErsatzTV.Core.Domain
{
    // used for shows split across sources (folders)
    // won't need to convert, since shows will be linked via metadata
    public class TelevisionShowSource
    {
        public int Id { get; set; }
        public int TelevisionShowId { get; set; }
        public TelevisionShow TelevisionShow { get; set; }
    }
}
