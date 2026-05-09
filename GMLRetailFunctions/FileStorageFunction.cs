using System.Net;
using System.Text.Json;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace GMLRetailFunctions;

public class FileStorageFunction
{
    private readonly ILogger<FileStorageFunction> _logger;

    public FileStorageFunction(ILogger<FileStorageFunction> logger)
    {
        _logger = logger;
    }

    public class FileRequest
    {
        public string? FileName { get; set; }
        public string? ContentBase64 { get; set; }
    }

    [Function("UploadLog")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "upload-log")] HttpRequestData req)
    {
        _logger.LogInformation("UploadLog invoked");

        var connectionString = Environment.GetEnvironmentVariable("AzureStorage");
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonSerializer.Deserialize<FileRequest>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var bytes = Convert.FromBase64String(input?.ContentBase64 ?? string.Empty);

        var shareClient = new ShareClient(connectionString, "logfiles");
        await shareClient.CreateIfNotExistsAsync();

        var rootDirectory = shareClient.GetRootDirectoryClient();
        var fileClient = rootDirectory.GetFileClient(input?.FileName);

        using var stream = new MemoryStream(bytes);
        await fileClient.CreateAsync(bytes.Length);
        if (bytes.Length > 0)
        {
            await fileClient.UploadAsync(stream);
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(new { status = "uploaded", fileName = input?.FileName }));
        return response;
    }
}
