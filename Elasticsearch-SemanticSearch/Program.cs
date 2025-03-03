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

class Program
{
    private const string NomicEmbedApiUrl = "http://localhost:11434/api/embeddings";
    private const string ElasticsearchUrl = "https://localhost:9200";
    private const string IndexName = "semantic_search_ty_product_knn_norm_cosine2";
    private static readonly string ElasticsearchUser = "elastic";
    private static readonly string ElasticsearchPassword = "M1DW3gREyHfrvdzbvvVU";

    private static readonly HttpClient _httpClient = new HttpClient();
    private static readonly ElasticsearchClient _esClient = CreateElasticClient();
    private static readonly MLContext mlContext = new MLContext();

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
        //        var embeddingNormalizedString = PreprocessText($"{record.ProductName} {record.CategoryName}");
        //        float[] embedding = GetEmbedding(embeddingNormalizedString).Result;
        //        SaveToElasticsearch(id, embedingString, embeddingNormalizedString, embedding);
        //    }
        //}
        // Verileri dbden al ve Elasticsearch'e kaydet
        //https://localhost:9200/semantic_search_ty/_count
        //https://localhost:9200/semantic_search_ty/_search

        Console.WriteLine("Arama yapmak için bir kelime girin:");
        string query = Console.ReadLine();

        //örnek : "pantul"
        //örnek : "erkek çocuk kot pantolon"
        //kategori örnek : temıslene
        //kategori örnek : giysi
        SearchInElasticsearch(PreprocessText(query));
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
                        .Dims(768)
                        .Similarity(Elastic.Clients.Elasticsearch.Mapping.DenseVectorSimilarity.Cosine)
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

    static void SearchInElasticsearch(string query)
    {
        float[] queryVector = GetEmbedding(query).Result;

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

    static string NormalizeText(string text)
    {
        var emptySamples = new List<TextData>();

        // Convert sample list to an empty IDataView.
        var emptyDataView = mlContext.Data.LoadFromEnumerable(emptySamples);

        // A pipeline for normalizing text.
        var normTextPipeline = mlContext.Transforms.Text.NormalizeText(
            "NormalizedText", "Text", TextNormalizingEstimator.CaseMode.Lower,
            keepDiacritics: false,
            keepPunctuations: false,
            keepNumbers: false);

        // Fit to data.
        var normTextTransformer = normTextPipeline.Fit(emptyDataView);

        // Create the prediction engine to get the normalized text from the
        // input text/string.
        var predictionEngine = mlContext.Model.CreatePredictionEngine<TextData,
            TransformedTextData>(normTextTransformer);

        // Call the prediction API.
        var data = new TextData()
        {
            Text = text
        };

        var prediction = predictionEngine.Predict(data);
       
        // Print the normalized text.
        return prediction.NormalizedText;
    }
    static string RemoveStopWords(string text)
    {
        var emptySamples = new List<TextData>();

        // Convert sample list to an empty IDataView.
        var emptyDataView = mlContext.Data.LoadFromEnumerable(emptySamples);

        // A pipeline for removing stop words from input text/string.
        // The pipeline first tokenizes text into words then removes stop words.
        // The 'RemoveStopWords' API ignores casing of the text/string e.g. 
        // 'tHe' and 'the' are considered the same stop words.
        var textPipeline = mlContext.Transforms.Text.TokenizeIntoWords("Words",
            "Text")
            .Append(mlContext.Transforms.Text.RemoveStopWords(
            "WordsWithoutStopWords", "Words", stopwords:
            new[] { "fakat", "lakin", "ancak", "acaba", "ama", "aslında", "az", "bazı", "belki", "biri", "birkaç", "birşey", "biz", "bu", "çok", "çünkü", "da", "daha", "de", "defa", "diye", "eğer", "en", "gibi", "hem", "hep", "hepsi", "her", "hiç", "için", "ile", "ise", "kez", "ki", "kim", "mı", "mu", "mü", "nasıl", "ne", "neden", "nerde", "nerede", "nereye", "niçin", "niye", "o", "sanki", "şey", "siz", "şu", "tüm", "ve", "veya", "ya", "yani" }));

        // Fit to data.
        var textTransformer = textPipeline.Fit(emptyDataView);

        // Create the prediction engine to remove the stop words from the input
        // text /string.
        var predictionEngine = mlContext.Model.CreatePredictionEngine<TextData,
            TransformedTextData>(textTransformer);

        // Call the prediction API to remove stop words.
        var data = new TextData()
        {
            Text = text
        };

        var prediction = predictionEngine.Predict(data);

        // Print the word vector without stop words.
       return string.Join(",", prediction.WordsWithoutStopWords);
    }
    static string RemoveDuplicateWords(string text)
    {
        // Split the text into words.
        var words = text.Split(',');
        // Remove duplicate words.
        var uniqueWords = words.Distinct();
        // Join the unique words back into a single string.
        return string.Join(",", uniqueWords);
    }
    static string PreprocessText(string text)
    {
        // Normalize the text.
        var normalizedText = NormalizeText(text);
        // Remove stop words from the normalized text.
        var wordsWithoutStopWords = RemoveStopWords(normalizedText);
        var uniqueWords = RemoveDuplicateWords(wordsWithoutStopWords);
        return uniqueWords;
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
    public string NormalizedText { get; set; }
}
