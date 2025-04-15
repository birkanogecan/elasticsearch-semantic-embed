using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Nest;
using static System.Net.Mime.MediaTypeNames;

namespace Elasticsearch_SemanticSearch.DefaultSemanticIndexer
{
    internal class Program
    {
        private static string ElasticsearchUrl = "https://localhost:9200";
        private static string IndexName = "semantic_search_ty_product_elasticsemantic";
        private static readonly string ElasticsearchUser = "elastic";
        private static readonly string ElasticsearchPassword = "M1DW3gREyHfrvdzbvvVU";

        private static readonly ElasticsearchClient _esClient = CreateElasticClient();
        static void Main(string[] args)
        {
            // Verileri dbden al ve Elasticsearch'e kaydet
            CreateElasticsearchIndex();
            List<ElasticSemanticRecord> elasticSemanticRecords = new List<ElasticSemanticRecord>();
            using (var context = new AppDbContext())
            {
                var productTYs = context.ProductTYs.ToList();
                foreach (var record in productTYs)
                {
                    var embeddingNormalizedString = NlpProvider.PreprocessText($"{record.ProductName} {record.CategoryName}");

                    var document = new ElasticSemanticRecord
                    {
                        Content = embeddingNormalizedString,
                    };

                    elasticSemanticRecords.Add(document);
                }

                SaveToElasticsearch(elasticSemanticRecords);
            }
            // Verileri dbden al ve Elasticsearch'e kaydet
        }
        static ElasticsearchClient CreateElasticClient()
        {
            var settings = new ElasticsearchClientSettings(new Uri(ElasticsearchUrl))
                .Authentication(new BasicAuthentication(ElasticsearchUser, ElasticsearchPassword))
                .ServerCertificateValidationCallback((sender, cert, chain, errors) => true)
                .DisableDirectStreaming(true)
                .DefaultIndex(IndexName);
            return new ElasticsearchClient(settings);
        }
        static void CreateElasticsearchIndex()
        {
            var existsResponse = _esClient.Indices.ExistsAsync(IndexName).Result;
            if (existsResponse.Exists)
            {
                Console.WriteLine("Elasticsearch index already exists.");
                return;
            }

            var response = _esClient.Indices.CreateAsync<ElasticSemanticRecord>(index => index
                .Index(IndexName)
                .Mappings(mappings => mappings
                    .Properties(properties => properties
                        .SemanticText(x => x.Content)
                        )
                    )
                ).Result;

            Console.WriteLine(response.Acknowledged ? "Elasticsearch index created successfully." : "Failed to create Elasticsearch index!");
        }

        static void SaveToElasticsearch(List<ElasticSemanticRecord> elasticSemanticRecords)
        {
            BulkRequestDescriptor bulkRequestDescriptor = new BulkRequestDescriptor();
            foreach (var record in elasticSemanticRecords)
            {
                bulkRequestDescriptor.Index<ElasticSemanticRecord>(record, idx => idx.Index(IndexName));
            }

            _esClient.BulkAsync(bulkRequestDescriptor);
        }
    }

    internal class ElasticSemanticRecord
    {
        public string Content { get; set; }
    }
}
