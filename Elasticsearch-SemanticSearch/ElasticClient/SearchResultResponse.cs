namespace Elasticsearch_SemanticSearch.ElasticClient
{
    public class SearchResultResponse
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string NormalizedContent { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public double? Score { get; set; }
    }
}
