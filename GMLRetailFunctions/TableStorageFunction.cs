using System.Net;
using System.Text.Json;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace GMLRetailFunctions;

public class TableStorageFunction
{
    private readonly ILogger<TableStorageFunction> _logger;

    public TableStorageFunction(ILogger<TableStorageFunction> logger)
    {
        _logger = logger;
    }

    public class CustomerRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    public class CustomerEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    [Function("StoreCustomer")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "store-customer")] HttpRequestData req)
    {
        _logger.LogInformation("StoreCustomer invoked");

        var connectionString = Environment.GetEnvironmentVariable("AzureStorage");
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonSerializer.Deserialize<CustomerRequest>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var entity = new CustomerEntity
        {
            PartitionKey = "Customer",
            RowKey = Guid.NewGuid().ToString(),
            FullName = input?.FullName,
            Email = input?.Email,
            Phone = input?.Phone,
            Address = input?.Address
        };

        var serviceClient = new TableServiceClient(connectionString);
        var tableClient = serviceClient.GetTableClient("customers");
        await tableClient.CreateIfNotExistsAsync();
        await tableClient.AddEntityAsync(entity);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(entity));
        return response;
    }
}
