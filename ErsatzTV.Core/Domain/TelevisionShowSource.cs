namespace ErsatzTV.Core.Domain
{
    public class TelevisionShowSource
    {
        public int Id { get; set; }
        public int TelevisionShowId { get; set; }
        public TelevisionShow TelevisionShow { get; set; }
    }
}
