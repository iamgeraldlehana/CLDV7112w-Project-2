using System.Net;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace GMLRetailFunctions;

public class QueueStorageFunction
{
    private readonly ILogger<QueueStorageFunction> _logger;

    public QueueStorageFunction(ILogger<QueueStorageFunction> logger)
    {
        _logger = logger;
    }

    public class QueueRequest
    {
        public string? Message { get; set; }
    }

    [Function("EnqueueOrder")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "enqueue-order")] HttpRequestData req)
    {
        _logger.LogInformation("EnqueueOrder invoked");

        var connectionString = Environment.GetEnvironmentVariable("AzureStorage");
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonSerializer.Deserialize<QueueRequest>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var queueClient = new QueueClient(connectionString, "orders", new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64
        });
        await queueClient.CreateIfNotExistsAsync();
        await queueClient.SendMessageAsync(input?.Message ?? string.Empty);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(new { status = "enqueued", message = input?.Message }));
        return response;
    }
}
