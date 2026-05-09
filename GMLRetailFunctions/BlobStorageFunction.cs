using System.Net;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace GMLRetailFunctions;

public class BlobStorageFunction
{
    private readonly ILogger<BlobStorageFunction> _logger;

    public BlobStorageFunction(ILogger<BlobStorageFunction> logger)
    {
        _logger = logger;
    }

    public class BlobRequest
    {
        public string? FileName { get; set; }
        public string? ContentBase64 { get; set; }
    }

    [Function("UploadBlob")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "upload-blob")] HttpRequestData req)
    {
        _logger.LogInformation("UploadBlob invoked");

        var connectionString = Environment.GetEnvironmentVariable("AzureStorage");
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonSerializer.Deserialize<BlobRequest>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var bytes = Convert.FromBase64String(input?.ContentBase64 ?? string.Empty);

        var serviceClient = new BlobServiceClient(connectionString);
        var containerClient = serviceClient.GetBlobContainerClient("mediafiles");
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobClient = containerClient.GetBlobClient(input?.FileName);
        using var stream = new MemoryStream(bytes);
        await blobClient.UploadAsync(stream, overwrite: true);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(new { blobName = input?.FileName }));
        return response;
    }
}
