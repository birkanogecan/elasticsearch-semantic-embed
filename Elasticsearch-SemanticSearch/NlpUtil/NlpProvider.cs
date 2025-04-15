using Microsoft.ML.Transforms.Text;
using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Elasticsearch_SemanticSearch.NlpUtil
{
    public static class NlpProvider
    {
        private static string NomicEmbedApiUrl = "http://localhost:11434/api/embeddings";
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly MLContext mlContext = new MLContext();
        public static float[] GetEmbedding(string text)
        {
            text = PreprocessText(text);

            var request = new
            {
                model = "snowflake-arctic-embed2",
                prompt = text
            };

            var response = _httpClient.PostAsJsonAsync(NomicEmbedApiUrl, request).Result;
            response.EnsureSuccessStatusCode();

            var responseData = response.Content.ReadFromJsonAsync<NomicResponse>().Result;
            return responseData.embedding; // İlk embedding'i al
        }

        static string NormalizeText(string text)
        {
            var emptySamples = new List<TextData>();

            // Convert sample list to an empty IDataView.
            var emptyDataView = mlContext.Data.LoadFromEnumerable(emptySamples);

            // A pipeline for normalizing text.
            var normTextPipeline = mlContext.Transforms.Text.NormalizeText(
                "NormalizedText", "Text", TextNormalizingEstimator.CaseMode.Lower,
                keepDiacritics: true,
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
            return string.Join(" ", uniqueWords);
        }
        public static string PreprocessText(string text)
        {
            // Normalize the text.
            var normalizedText = NormalizeText(text);
            // Remove stop words from the normalized text.
            var wordsWithoutStopWords = RemoveStopWords(normalizedText);
            var uniqueWords = RemoveDuplicateWords(wordsWithoutStopWords);
            return uniqueWords;
        }
    }
    public class NomicResponse
    {
        public float[] embedding { get; set; }
    }
}
