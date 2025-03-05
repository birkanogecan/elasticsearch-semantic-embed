using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CsvHelper;
using Elasticsearch.Net;
using Elasticsearch_SemanticSearch;
using Nest;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Elastic.Clients.Elasticsearch.Nodes;
using Microsoft.ML;
using Microsoft.ML.Transforms.Text;

public class Program
{
    
    private static string ElasticsearchUrl = "https://localhost:9200";
    private static string IndexName = "semantic_search_ty_product_knn_norm_snow2_l2"; //semantic_search_ty_product_knn_norm_snow (cosine), semantic_search_ty_product_knn_norm_snow_l2 (knn(l2), "semantic_search_ty_product_knn_norm_snow2" (cosine), "semantic_search_ty_product_knn_norm_snow2_l2" (knn(l2))
    private static readonly string ElasticsearchUser = "elastic";
    private static readonly string ElasticsearchPassword = "M1DW3gREyHfrvdzbvvVU";

    private static readonly ElasticsearchClient _esClient = CreateElasticClient();

    static void Main()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Console.InputEncoding = Encoding.GetEncoding("Windows-1254");
        Console.OutputEncoding = Encoding.GetEncoding("Windows-1254");

        // Verileri dbden al ve Elasticsearch'e kaydet
        //int id = 0;
        //CreateElasticsearchIndex();
        //using (var context = new AppDbContext())
        //{
        //    var productTYs = context.ProductTYs.ToList();
        //    foreach (var record in productTYs)
        //    {
        //        id++;
        //        var embedingString = $"Ürünün başlığı: {record.ProductName}, Ürünün kategorisi: {record.CategoryName}, Ürünün markası: {record.BrandName}";
        //        var embeddingNormalizedString = NlpProvider.PreprocessText($"{record.ProductName} {record.CategoryName}");
        //        float[] embedding = NlpProvider.GetEmbedding(embeddingNormalizedString).Result;

        //        SaveToElasticsearch(id, embedingString, embeddingNormalizedString, embedding);
        //    }
        //}
        // Verileri dbden al ve Elasticsearch'e kaydet
        //https://localhost:9200/semantic_search_ty/_count
        //https://localhost:9200/semantic_search_ty/_search

        Console.WriteLine("Arama yapmak için bir kelime girin:");
        string query = Console.ReadLine();

        //örnek : pantul
        //örnek : göynek
        //örnek : küçük pantolon
        //küçük insan
        //kategori örnek : temıslene
        //kategori örnek : giysi
        //sıcak havalarda giyilecek giysi
        //soğuk havalarda giyilecek giysi
        //yumuşak kumaş
        //para
        //para taşımak için eşya
        //bayan giyim
        //adam
        //adamlar için soğukta giyilecek giysi
        //taşıt
        //araba oturak örtüsü
        //elektronik

        SearchInElasticsearch(query);
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

        var response = _esClient.Indices.CreateAsync<ElasticRecord>(index => index
            .Index(IndexName)
            .Mappings(mappings => mappings
                .Properties(properties => properties
                    .IntegerNumber(x => x.Id)
                    .Text(x => x.Content)
                    .Text(x => x.NormalizedText)
                    .DenseVector(x => x.Vector, dv => dv
                        .Dims(1024)
                        .Similarity(Elastic.Clients.Elasticsearch.Mapping.DenseVectorSimilarity.L2Norm)
                        .Index(true)
                    )
                )
            )
        ).Result;

        Console.WriteLine(response.Acknowledged ? "Elasticsearch index created successfully." : "Failed to create Elasticsearch index!");
    }

    static void SaveToElasticsearch(int id, string text, string normalizedText, float[] vector)
    {
        var document = new ElasticRecord
        {
            Id = id,
            Content = text,
            NormalizedText = normalizedText,
            Vector = vector
        };
        _esClient.IndexAsync(document, i => i.Id(id).Index(IndexName));
    }

    public static void SearchInElasticsearch(string query)
    {
        float[] queryVector = NlpProvider.GetEmbedding(query);

        var searchResponse =  _esClient.SearchAsync<ElasticRecord>(s => s
                .Index(IndexName)
                .Size(10)
                .Query(q => q.Knn(k => k
                    .Field(f => f.Vector)
                    .QueryVector(queryVector)
                    .k(10)
                    .NumCandidates(10)
                ))
            ).Result;
       
        Console.WriteLine("En Benzer Sonuçlar:");
        foreach (var hit in searchResponse.Hits)
        {
            
            Console.WriteLine($"Skor : {hit.Score}");
            Console.WriteLine($"Id : {hit.Source.Id}");
            Console.WriteLine($"İçerik : {hit.Source.Content}");
            Console.WriteLine($"Normalize : {hit.Source.NormalizedText}");
            Console.WriteLine("---------------------------------------------------------------");
        }
    }

   
}

public class ProductRecord
{
    public string title { get; set; }
  
}


public class ElasticRecord
{
    public int Id { get; set; }
    public string Content { get; set; }
    public float[] Vector { get; set; }
    public string NormalizedText { get; set; }
}
