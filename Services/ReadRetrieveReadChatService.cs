// Copyright (c) Microsoft. All rights reserved.

using AIHireDocumentService.shared.Shared.Models;
using AiHireService.Service;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using MinimalApi.Extensions;
using Newtonsoft.Json;
using Shared.Models;
using System.Text;
using System;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Newtonsoft.Json.Linq;
using System.Net;

namespace MinimalApi.Services;
#pragma warning disable SKEXP0011 // Mark members as static
#pragma warning disable SKEXP0001 // Mark members as static
public class ReadRetrieveReadChatService : IReadRetrieveReadChatService
{
    private readonly ISearchService _searchClient;
    private readonly OpenAIClient _openAIClient;
    private readonly Kernel _kernel;
    private readonly IConfiguration _configuration;

    public ReadRetrieveReadChatService(
        ISearchService searchClient,
        OpenAIClient client,
        IConfiguration configuration)
    {
        _searchClient = searchClient;
        _openAIClient = client;
        var kernelBuilder = Kernel.CreateBuilder();

       
            var deployedModelName = configuration["AzureOpenAiChatGptDeployment"];
            var embeddingModelName = configuration["AzureOpenAiEmbeddingDeployment"];
            if (!string.IsNullOrEmpty(embeddingModelName))
            {
                var endpoint = configuration["AzureOpenAiServiceEndpoint"];
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                kernelBuilder = kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(embeddingModelName,client);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                kernelBuilder = kernelBuilder.AddAzureOpenAIChatCompletion(deployedModelName,client);
            }
        

        _kernel = kernelBuilder.Build();
        _configuration = configuration;
    }

    public async Task<SearchResponse> ReplyAsync(
        string userQuestion,
        RequestOverrides? overrides,
        CancellationToken cancellationToken = default)
    {
        var deployedModelName = _configuration["AzureOpenAiChatGptDeployment"];
        var embeddingModelName = _configuration["AzureOpenAiEmbeddingDeployment"];
        var top = overrides?.Top ?? 3;
        var useSemanticCaptions = overrides?.SemanticCaptions ?? false;
        var useSemanticRanker = overrides?.SemanticRanker ?? false;
        var excludeCategory = overrides?.ExcludeCategory ?? null;
        var filter = excludeCategory is null ? null : $"category ne '{excludeCategory}'";
        var chat = new AzureOpenAIChatCompletionService(deployedModelName, _openAIClient);
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var embedding = new AzureOpenAITextEmbeddingGenerationService(embeddingModelName, _openAIClient);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        float[]? embeddings = null;
        var question = userQuestion;
      
        string? query = null;
        var getQueryChat = new ChatHistory(@"You are a helpful AI assistant, generate search query for followup question.
Make your respond simple and precise. Return the query only, do not return any other text.
e.g.
Northwind Health Plus AND standard plan.
standard plan AND dental AND employee benefit.
");

        getQueryChat.AddUserMessage(question);
        var result = await chat.GetChatMessageContentAsync(
            getQueryChat,
            executionSettings: new()
            {
                ModelId= "gpt-4-32k"
            },
            cancellationToken: cancellationToken);

        query = result.Content ?? "";

        if (overrides?.RetrievalMode != RetrievalMode.Text && embedding is not null)
        {
            EmbeddingInput emb = new EmbeddingInput()
            {
                input = (!string.IsNullOrEmpty(query) ? query : question)
            };
            Uri uri = new Uri("");
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(JsonConvert.SerializeObject(emb), Encoding.UTF8, "application/json")



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
                        var res = JsonConvert.DeserializeObject<EmbeddingResponse>(xmlString);
                        embeddings = res.data[0].embedding;


                    }
                }
            };
           //embeddings = (await embedding.GenerateEmbeddingAsync(!string.IsNullOrEmpty(query) ? query: question, cancellationToken: cancellationToken)).ToArray();
        }

        return await _searchClient.QueryDocumentsAsync(query, embeddings, overrides, cancellationToken);

         
    }
}
