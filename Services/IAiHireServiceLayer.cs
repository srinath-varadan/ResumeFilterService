
using Azure.Storage.Blobs.Models;

namespace AiHireService.Service
{
    public interface IAiHireServiceLayer
    {
        Task UploadFiles(string inputPath);
    }
}
