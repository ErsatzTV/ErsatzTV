namespace ErsatzTV;

public static class SearchHelper
{
    public static string ElasticSearchUri { get; private set; }
    public static string ElasticSearchIndexName { get; private set; }
    public static bool IsElasticSearchEnabled { get; private set; }

    public static void Init(IConfiguration configuration)
    {
        ElasticSearchUri = configuration["ElasticSearch:Uri"];
        ElasticSearchIndexName = configuration["ElasticSearch:IndexName"];
        if (string.IsNullOrWhiteSpace(ElasticSearchIndexName))
        {
            ElasticSearchIndexName = "ersatztv";
        }

        IsElasticSearchEnabled = !string.IsNullOrWhiteSpace(ElasticSearchUri);
    }
}
