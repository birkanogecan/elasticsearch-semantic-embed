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
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Elastic.Clients.Elasticsearch.Nodes;
using Microsoft.ML;
using Microsoft.ML.Transforms.Text;
using Elasticsearch_SemanticSearch.NlpUtil;
using Elasticsearch_SemanticSearch.ElasticClient;
using Elasticsearch_SemanticSearch.Data;

public class Program
{
    static void Main()
    {
        // Verileri dbden al ve Elasticsearch'e kaydet
        int id = 0;
        ESClient.CreateElasticsearchIndex();

        using (var context = new AppDbContext())
        {
            var productTYs = context.ProductTYs.ToList();
            foreach (var item in productTYs)
            {
                id++;
                ElasticRecord record = new ElasticRecord();

                record.Id = id;
                record.Brand = item.BrandName;
                record.Category = item.CategoryName;

                record.Content = $"Ürünün başlığı: {item.ProductName}, Ürünün kategorisi: {item.CategoryName}, Ürünün markası: {item.BrandName}";
                record.NormalizedText = NlpProvider.PreprocessText($"{item.ProductName} {item.CategoryName}");
                record.Vector = NlpProvider.GetEmbedding(record.NormalizedText);

                ESClient.SaveToElasticsearch(record);
            }
        }

        // Verileri dbden al ve Elasticsearch'e kaydet
        //https://localhost:9200/semantic_search_ty/_count
        //https://localhost:9200/semantic_search_ty/_search
    }
}
