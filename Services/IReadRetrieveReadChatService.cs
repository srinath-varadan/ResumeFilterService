
using AIHireDocumentService.shared.Shared.Models;
using Azure.Storage.Blobs.Models;
using Shared.Models;

namespace AiHireService.Service
{
    public interface IReadRetrieveReadChatService
    {
        Task<SearchResponse> ReplyAsync(
        string userQuestion,
        RequestOverrides? overrides,
        CancellationToken cancellationToken = default);
    }
}
