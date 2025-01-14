using Newtonsoft.Json;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace AIHireDocumentService.shared.Shared.Models
{
    public class SearchRequest
    {
        public string context { get; set; }
        public string? category { get; set; }
        public double threshold { get; set; }
        public int noOfMatches { get; set; }
        public string inputPath { get; set; }
    }
    public class SearchResponse
    {
        public SearchResponse()
        {
            metadata = new Metadata();
            results = new List<ResultsResponse>();
        }
        public string status { get; set; }
        public Metadata metadata { get; set; }
        public int count { get; set; }
        public List<ResultsResponse> results { get; set; }
    }

    public class Metadata
    {
        public double ? confidenceScore { get; set; }
    }

    public class Results: ResultsResponse
    {

        public double? confidenceScore { get; set; }
    }

    public class ResultsResponse
    {
        public int id { get; set; }
        public double? score { get; set; }
        public string path { get; set; }
    }

    public class SearchRequestIndex
    {
        public string search { get; set; }
        public string searchFields { get; set; }
        public string semanticConfiguration { get; set; }
        public string queryType { get; set; }
        [DisplayNameAttribute("select")]
        public string select { get; set; }

        public string searchMode { get; set; }
        public string vectorFilterMode { get; set; }

        public Vector[] vectorQueries { get; set; }

    }

    public class SearchResultObject
    {

        public List<SearchResultValue> value { get; set; }
    }

    public class SearchResultValue
    {
        [JsonProperty("@search.score")]
        public double searchScore { get; set; }

        [JsonProperty("@search.rerankerScore")]
        public double confidenceScore { get; set; }

        public string content { get; set; }

        public string sourcefile { get; set; }
    }

    public class Vector
    {

        public string kind { get; set; }
        public float[] vector { get; set; }
        public string fields { get; set; }
        public int k { get; set; }
    }

    public class EmbeddingInput
    {
       public string input { get; set; }
    }

    public class EmbeddingResponse
    {
        public List<EmbResult> data { get; set; }
    }

    public class EmbResult
    {
        public float[] embedding { get; set; }
    }

}
