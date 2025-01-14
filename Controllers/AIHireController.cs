using AIHireDocumentService.shared.Shared.Models;
using AiHireService.Service;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Services;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace AIHireDocumentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AIHireController : ControllerBase
    {
        private readonly IAiHireServiceLayer _service;

        private readonly IReadRetrieveReadChatService _chatService;

        public AIHireController(IAiHireServiceLayer service, IReadRetrieveReadChatService chatService)
        {
            _service = service;
            _chatService = chatService;
        }

        #region Search
        [HttpPost]
        [Route("Search")]
        public async Task<IActionResult> Search(SearchRequest searchRequest)
        {
            if (!string.IsNullOrEmpty(searchRequest.context)) 
            {
                if (!string.IsNullOrEmpty(searchRequest.inputPath) && Uri.IsWellFormedUriString(searchRequest.inputPath, UriKind.RelativeOrAbsolute))
                    await _service.UploadFiles(searchRequest.inputPath);

                var response = await _chatService.ReplyAsync(searchRequest.context, new Shared.Models.RequestOverrides()
                {
                    Top = searchRequest.noOfMatches,
                    SemanticCaptions = true,
                    SemanticRanker = true,
                    Threshold = searchRequest.threshold
                });
                return Ok(response);
            }

            return Ok();
        }
        #endregion

        private static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

    }
}
