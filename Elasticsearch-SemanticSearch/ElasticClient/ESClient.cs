using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Elasticsearch_SemanticSearch.NlpUtil;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elasticsearch_SemanticSearch.ElasticClient
{
    public static class ESClient
    {
        private static readonly string ElasticsearchUser = "elastic";
        private static readonly string ElasticsearchPassword = "M1DW3gREyHfrvdzbvvVU";
        private static string ElasticsearchUrl = "https://localhost:9200";
        private static string IndexName = "semantic_search_ty_product_knn_norm_snow2_l2"; //"semantic_search_ty_product_knn_norm_snow2_l2" "semantic_search_ty_product_new1"
        private static readonly ElasticsearchClient _esClient = CreateElasticClient();

        public static ElasticsearchClient CreateElasticClient()
        {
            var settings = new ElasticsearchClientSettings(new Uri(ElasticsearchUrl))
                .Authentication(new BasicAuthentication(ElasticsearchUser, ElasticsearchPassword))
                .ServerCertificateValidationCallback((sender, cert, chain, errors) => true)
                .DisableDirectStreaming(true)
                .DefaultIndex(IndexName);
            return new ElasticsearchClient(settings);
        }

        public static List<ElasticRecord> SearchInElasticsearch(string query)
        {
            float[] queryVector = NlpProvider.GetEmbedding(query);

            var searchResponse = _esClient.SearchAsync<ElasticRecord>(s => s
                    .Index(IndexName)
                    .Size(10)
                    //.Query(q => q
                    //    .Match(f => f
                    //    .Query(query)
                    //    .Field(t => t.Category)
                    //    .Boost((float)0.9)))
                    .Knn(k => k
                        .Field(f => f.Vector)
                        .QueryVector(queryVector)
                        .k(5)
                        .NumCandidates(10)
                        //.Boost((float)0.1)
                    )
                ).Result;
           
            List<ElasticRecord> elasticRecords = searchResponse.Hits
                .Select(hit =>
                {
                    var record = hit.Source;
                    record.Score = hit.Score;
                    return record;
                })
                .ToList();

            return elasticRecords;
        }

        public static void SaveToElasticsearch(ElasticRecord elasticRecord)
        {
            var result = _esClient.IndexAsync(elasticRecord);
        }

        public static void CreateElasticsearchIndex()
        {
            var existsResponse = _esClient.Indices.ExistsAsync(IndexName).Result;
            if (existsResponse.Exists)
            {
                Console.WriteLine("Elasticsearch index already exists.");
                return;
            }

            var response = _esClient.Indices.CreateAsync<ElasticRecord>(index => index
                .Index(IndexName)
                .Mappings(mappings => mappings
                    .Properties(properties => properties
                        .IntegerNumber(x => x.Id)
                        .Text(x => x.Content)
                        .Text(x => x.Brand)
                        .Text(x => x.Category)
                        .Text(x => x.NormalizedText)
                        .DenseVector(x => x.Vector, dv => dv
                            .Dims(1024)
                            .Similarity(Elastic.Clients.Elasticsearch.Mapping.DenseVectorSimilarity.L2Norm)
                            .Index(true)
                        )
                    )
                )
            ).Result;
        }
    }
}
