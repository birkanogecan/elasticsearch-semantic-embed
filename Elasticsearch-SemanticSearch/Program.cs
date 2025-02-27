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
using Nest;

class Program
{
    private const string NomicEmbedApiUrl = "http://localhost:11434/api/embeddings";
    private const string ElasticsearchUrl = "https://localhost:9200";
    private const string IndexName = "semantic_search6";
    private static readonly string ElasticsearchUser = "elastic";
    private static readonly string ElasticsearchPassword = "M1DW3gREyHfrvdzbvvVU";

    private static readonly HttpClient _httpClient = new HttpClient();
    private static readonly ElasticClient _esClient = CreateElasticClient();

    static void Main()
    {
        // Verileri oku ve Elasticsearch'e kaydet
        //int id = 0;
        //var records = ReadCsv("products.csv");
        //CreateElasticsearchIndex();
        //foreach (var record in records)
        //{
        //    id++;
        //    float[] embedding = GetEmbedding(record.title).Result;
        //    SaveToElasticsearch(id, record.title, embedding);
        //}
        // Verileri oku ve Elasticsearch'e kaydet

        Console.WriteLine("Arama yapmak için bir kelime girin:");
        string query = Console.ReadLine();

        //örnek : "pantul"
        SearchInElasticsearch(query);
    }

    static ElasticClient CreateElasticClient()
    {
        var settings = new ConnectionSettings(new Uri(ElasticsearchUrl))
            .BasicAuthentication(ElasticsearchUser, ElasticsearchPassword)
            .ServerCertificateValidationCallback((sender, cert, chain, errors) => true)
            .DisableDirectStreaming(true)
            .DefaultIndex(IndexName);
        return new ElasticClient(settings);
    }

    static List<ProductRecord> ReadCsv(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        return csv.GetRecords<ProductRecord>().ToList();
    }

    static async Task<float[]> GetEmbedding(string text)
    {
        var request = new
        {
            model = "nomic-embed-text",
            prompt = text
        };

        var response = await _httpClient.PostAsJsonAsync(NomicEmbedApiUrl, request);
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadFromJsonAsync<NomicResponse>();
        return responseData.embedding; // İlk embedding'i al
    }

    static void CreateElasticsearchIndex()
    {
        var existsResponse =  _esClient.Indices.ExistsAsync(IndexName).Result;
        if (existsResponse.Exists)
        {
            Console.WriteLine("Elasticsearch index zaten mevcut.");
            return;
        }

        var mappingJson = @"
    {
        ""mappings"": {
            ""properties"": {
                ""id"": { ""type"": ""integer"" },
                ""content"": { ""type"": ""text"" },
                ""vector"": {
                    ""type"": ""dense_vector"",
                    ""dims"": 768,
                    ""index"": true,
                    ""similarity"": ""cosine""
                }
            }
        }
    }";

        var response =  _esClient.LowLevel.Indices.CreateAsync<StringResponse>(IndexName, mappingJson).Result;
        Console.WriteLine(response.Success ? "Elasticsearch index başarıyla oluşturuldu." : "Elasticsearch index oluşturulamadı!");
    }

    static void SaveToElasticsearch(int id, string text, float[] vector)
    {
        var document = new ElasticRecord
        {
            Id = id,
            Content = text,
            Vector = vector
        };
        _esClient.Index(document, i => i.Id(id).Index(IndexName));
    }

    static void SearchInElasticsearch(string query)
    {
        float[] queryVector = GetEmbedding(query).Result;

        var searchResponse = _esClient.Search<ElasticRecord>(s => s
     .Size(5)
     .Query(q => q
         .ScriptScore(ss => ss
             .Query(qq => qq.MatchAll())
             .Script(script => script
                 .Source("cosineSimilarity(params.query_vector, 'vector') + 0.5") // 
                 .Params(p => p.Add("query_vector", queryVector))
             )
         )
     )
 );

        Console.WriteLine("En Benzer Sonuçlar:");
        foreach (var hit in searchResponse.Hits)
        {
            Console.WriteLine($"{hit.Source.Id} -  {hit.Source.Content}");
        }
    }
}

public class ProductRecord
{
    public string title { get; set; }
  
}

public class NomicResponse
{
    public float[] embedding { get; set; }
}
public class ElasticRecord
{
    public int Id { get; set; }
    public string Content { get; set; }
    public float[] Vector { get; set; }
}
