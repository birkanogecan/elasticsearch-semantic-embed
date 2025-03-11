
using Elasticsearch_SemanticSearch.ElasticClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Xml.Linq;

namespace Elasticsearch_SemanticSearch.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

           

            app.MapGet("/search", ([FromQuery(Name = "p")] string prompt) =>
            {
                var searchResponse =  ESClient.SearchInElasticsearch(prompt);
                List<SearchResultResponse> searchResults = new ();

                foreach (var item in searchResponse)
                {
                    SearchResultResponse searchResultResponse = new SearchResultResponse();
                    searchResultResponse.Id = item.Id;
                    searchResultResponse.Content = item.Content;
                    searchResultResponse.NormalizedContent = item.NormalizedText;
                    searchResultResponse.Brand = item.Brand;
                    searchResultResponse.Category = item.Category;
                    searchResultResponse.Score = item.Score;

                    searchResults.Add(searchResultResponse);
                }
                return searchResults;
            })
            .WithName("Search");

            app.Run();
        }
    }
}
