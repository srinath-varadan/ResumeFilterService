using AIHireDocumentService.shared.Shared.Models;
using Shared.Models;

public interface ISearchService
{
    Task<SearchResponse> QueryDocumentsAsync(
        string? query = null,
        float[]? embedding = null,
        RequestOverrides? overrides = null,
        CancellationToken cancellationToken = default);
}
