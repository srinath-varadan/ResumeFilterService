using AIHireDocumentService.shared.Shared.Models;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Components.Forms;
using Shared.Models;
using System.Net;
using System.Xml.Linq;
using Results = AIHireDocumentService.shared.Shared.Models.Results;
using Azure.Core;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class AzureSearchService : ISearchService
{
    SearchClient searchClient;
    public AzureSearchService(SearchClient _searchClient)
    {
        searchClient = _searchClient;
    }
    public async Task<SearchResponse> QueryDocumentsAsync(
        string? query = null,
        float[]? embedding = null,
        RequestOverrides? overrides = null,
        CancellationToken cancellationToken = default)
    {
        if (query is null && embedding is null)
        {
            throw new ArgumentException("Either query or embedding must be provided");
        }

        var documentContents = string.Empty;
        var top = overrides?.Top ?? 3;
        var exclude_category = overrides?.ExcludeCategory;
        var filter = exclude_category == null ? string.Empty : $"category ne '{exclude_category}'";
         var useSemanticRanker = overrides?.SemanticRanker ?? true;
        var useSemanticCaptions = overrides?.SemanticCaptions ?? true;

        Uri uri = new Uri("");
        Vector vectorObj = new Vector
        {
            kind = "vector",
            vector = embedding,
            fields = "embedding",
            k = 50
        };
        var vectorQ = new List<Vector>();
        vectorQ.Add(vectorObj);
       
        Byte[] requestPayload = null;
        SearchRequestIndex json = new SearchRequestIndex()
        {
            search = query,
            searchFields = "content",
            semanticConfiguration = "default",
            queryType = "semantic",
            searchMode = "all",
            vectorFilterMode = "preFilter",
            vectorQueries = vectorQ.ToArray()
        };
        int i = 1;
        var sb = new List<AIHireDocumentService.shared.Shared.Models.Results>();
        using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(JsonConvert.SerializeObject(json), Encoding.UTF8, "application/json")


       
        })
        {
            httpRequestMessage.Headers.Add("api-key", "");
            httpRequestMessage.Method = HttpMethod.Post;
            using (HttpResponseMessage httpResponseMessage =
             await new HttpClient().SendAsync(httpRequestMessage))
            {
                // If successful (status code = 200),
                //   parse the XML response for the container names.
                if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                {
                    System.String xmlString = await httpResponseMessage.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<SearchResultObject>(xmlString);
                    if (result != null && result.value.Any()) {
                        foreach (var res in result.value)
                        {

                            sb.Add(new Results()
                            {
                                path = res.sourcefile,
                                score = res.searchScore,
                                confidenceScore = res.confidenceScore,
                            });
                        }
                    }
                }
            }
        }

        List<Results> groupedResponse = sb.Where(x=>x.confidenceScore >= overrides.Threshold).OrderByDescending(y=>y.confidenceScore).GroupBy(x => x.path).Select(std => new AIHireDocumentService.shared.Shared.Models.Results
        {
            id = std.FirstOrDefault().id,
            path = std.FirstOrDefault().path,
            score = std.OrderByDescending(y=>y.score).FirstOrDefault().score,
            confidenceScore = std.OrderByDescending(y => y.confidenceScore).FirstOrDefault().confidenceScore
        }).ToList();

        return new SearchResponse()
        {
            status = "success",
            count = groupedResponse.Count(),
            metadata = new Metadata()
            {
                confidenceScore = groupedResponse.Max(y => y.confidenceScore)
            },
            results = groupedResponse.OrderByDescending(y => y.confidenceScore).Select(y => new ResultsResponse { id = y.id,path = y.path,score = y.score}).Take(top).ToList()
        };

    }

    /// <summary>
    /// query images.
    /// </summary>
    /// <param name="embedding">embedding for imageEmbedding</param>
    public async Task<SupportingImageRecord[]> QueryImagesAsync(
        string? query = null,
        float[]? embedding = null,
        RequestOverrides? overrides = null,
        CancellationToken cancellationToken = default)
    {
        var top = overrides?.Top ?? 3;
        var exclude_category = overrides?.ExcludeCategory;
        var filter = exclude_category == null ? string.Empty : $"category ne '{exclude_category}'";

        var searchOptions = new SearchOptions
        {
            Filter = filter,
            Size = top,
        };

        if (embedding != null)
        {
            var vectorQuery = new VectorizedQuery(embedding)
            {
                KNearestNeighborsCount = top,
            };
            vectorQuery.Fields.Add("imageEmbedding");
            searchOptions.VectorSearch = new();
            searchOptions.VectorSearch.Queries.Add(vectorQuery);
        }

        var searchResultResponse = await searchClient.SearchAsync<SearchDocument>(
                       query, searchOptions, cancellationToken);

        if (searchResultResponse.Value is null)
        {
            throw new InvalidOperationException("fail to get search result");
        }

        SearchResults<SearchDocument> searchResult = searchResultResponse.Value;
        var sb = new List<SupportingImageRecord>();

        foreach (var doc in searchResult.GetResults())
        {
            doc.Document.TryGetValue("sourcefile", out var sourceFileValue);
            doc.Document.TryGetValue("imageEmbedding", out var imageEmbeddingValue);
            doc.Document.TryGetValue("category", out var categoryValue);
            doc.Document.TryGetValue("content", out var imageName);
            if (sourceFileValue is string url &&
                imageName is string name &&
                categoryValue is string category &&
                category == "image")
            {
                sb.Add(new SupportingImageRecord(name, url));
            }
        }

        return [.. sb];
    }
}
