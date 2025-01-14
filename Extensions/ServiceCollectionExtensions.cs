// Copyright (c) Microsoft. All rights reserved.

using AiHireService.Service;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using MinimalApi.Services;

namespace MinimalApi.Extensions;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddAzureServices(this IServiceCollection services)
    {

        services.AddScoped<ISearchService, AzureSearchService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureSearchServiceEndpoint = config["AzureSearchServiceEndpoint"];
            ArgumentNullException.ThrowIfNullOrEmpty(azureSearchServiceEndpoint);

            var azureSearchIndex = config["AzureSearchIndex"];
            var azureSearchKey = config["AzureSearchKey"];
            ArgumentNullException.ThrowIfNullOrEmpty(azureSearchIndex);

            var searchClient = new SearchClient(
                               new Uri(azureSearchServiceEndpoint), azureSearchIndex, new Azure.AzureKeyCredential(azureSearchKey));

            return new AzureSearchService(searchClient);
        });

       

        services.AddScoped<OpenAIClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
                var azureOpenAiServiceEndpoint = config["AzureOpenAiServiceEndpoint"];
                var azureOpenAiServiceKey = config["AzureOpenAiServiceKey"];
                ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint);

                var openAIClient = new OpenAIClient(new Uri(azureOpenAiServiceEndpoint), new Azure.AzureKeyCredential(azureOpenAiServiceKey));

                return openAIClient;
        });

        services.AddScoped<IReadRetrieveReadChatService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureOpenAiServiceKey = config["AzureOpenAiServiceKey"];
            var useVision = false;
            var openAIClient = sp.GetRequiredService<OpenAIClient>();
            var searchClient = sp.GetRequiredService<ISearchService>();
             return new ReadRetrieveReadChatService(searchClient, openAIClient, config);
            
        });

        return services;
    }

    internal static IServiceCollection AddCrossOriginResourceSharing(this IServiceCollection services)
    {
        services.AddCors(
            options =>
                options.AddDefaultPolicy(
                    policy =>
                        policy.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod()));

        return services;
    }
}
