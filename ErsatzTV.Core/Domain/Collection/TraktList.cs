namespace ErsatzTV.Core.Domain;

public class TraktList
{
    public int Id { get; set; }
    public int TraktId { get; set; }
    public string User { get; set; }
    public string List { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int ItemCount { get; set; }
    public List<TraktListItem> Items { get; set; }
}