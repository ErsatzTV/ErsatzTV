namespace ErsatzTV.Core.Domain;

public class SmartCollection
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Query { get; set; }
    public List<MultiCollection> MultiCollections { get; set; }
    public List<MultiCollectionSmartItem> MultiCollectionSmartItems { get; set; }
}
