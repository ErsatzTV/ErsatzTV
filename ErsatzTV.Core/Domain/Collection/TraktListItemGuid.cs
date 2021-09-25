namespace ErsatzTV.Core.Domain
{
    public class TraktListItemGuid
    {
        public int Id { get; set; }
        public string Guid { get; set; }
        public int TraktListItemId { get; set; }
        public TraktListItem TraktListItem { get; set; }
    }
}
