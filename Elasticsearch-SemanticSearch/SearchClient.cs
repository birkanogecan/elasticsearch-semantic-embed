using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elasticsearch_SemanticSearch
{
    public static class SearchClient
    {
        private static readonly string ElasticsearchUser = "elastic";
        private static readonly string ElasticsearchPassword = "M1DW3gREyHfrvdzbvvVU";
        private static string ElasticsearchUrl = "https://localhost:9200";
        private static string IndexName = "semantic_search_ty_product_knn_norm_snow2_l2"; //semantic_search_ty_product_knn_norm_snow (cosine), semantic_search_ty_product_knn_norm_snow_l2 (knn(l2), "semantic_search_ty_product_knn_norm_snow2" (cosine), "semantic_search_ty_product_knn_norm_snow2_l2" (knn(l2))
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
                    .Query(q => q.Knn(k => k
                        .Field(f => f.Vector)
                        .QueryVector(queryVector)
                        .k(10)
                        .NumCandidates(10)
                    ))
                ).Result;

            return searchResponse.Hits.Select(hit => hit.Source).ToList();
        }
    }
}
