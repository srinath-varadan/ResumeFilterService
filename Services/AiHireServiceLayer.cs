using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using System.Net;
using System.Xml.Linq;

namespace AiHireService.Service
{
    public class AiHireServiceLayer : IAiHireServiceLayer
    {
        private static IConfigurationBuilder _builder;
        private static IConfigurationRoot _configuration;
        private static string index;
        private static string blobStorageConnectionString;
        private static string blobContainerName;
        private static BlobContainerClient _blobContainerClient;
        private static BlockBlobClient _blockblobcontainerclient;


        public AiHireServiceLayer()
        {
            // Create a configuration using appsettings.json
            _builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            _configuration = _builder.Build();

            // Read the values from appsettings.json
            blobStorageConnectionString = _configuration["StorageConnectionString"];
            blobContainerName = _configuration["StorageContainerName"];
            _blobContainerClient = new BlobContainerClient(blobStorageConnectionString, blobContainerName);
        }

        #region Upload
        public async Task UploadFiles(string inputPath)
        {

            Uri uri = new Uri(inputPath + "&restype=container");

            Byte[] requestPayload = null;
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri)
            {
                Content = (requestPayload == null) ? null : new ByteArrayContent(requestPayload)
            })
            {
                httpRequestMessage.Headers.Add("x-ms-date", DateTime.UtcNow.ToString("R"));
                httpRequestMessage.Headers.Add("x-ms-version", "2012-02-12");
                httpRequestMessage.Method = HttpMethod.Get;
                using (HttpResponseMessage httpResponseMessage =
                 await new HttpClient().SendAsync(httpRequestMessage))
                {
                    // If successful (status code = 200),
                    //   parse the XML response for the container names.
                    if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                    {
                        String xmlString = await httpResponseMessage.Content.ReadAsStringAsync();
                        XElement x = XElement.Parse(xmlString);
                        foreach (XElement container in x.Element("Blobs").Elements("Blob"))
                        {
                            Uri sourceUri = new(container.Element("Url").Value);
                            BlockBlobClient destBlob = _blobContainerClient.GetBlockBlobClient(container.Element("Name").Value);
                            // Start the copy operation and wait for it to complete
                            _ = destBlob.SyncUploadFromUriAsync(sourceUri);
                        }
                    }
                }
            }

        }
        #endregion

    }
}
