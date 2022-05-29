using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;

const string databaseId = "TestDatabase";
const string containerId = "MyContainer";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var endpointUrl = builder.Configuration["EndPointUrl"];
var authKey = builder.Configuration["AuthorizationKey"];

Database? database = null;
Container? container = null;

using (var client = new CosmosClient(endpointUrl, authKey))
{
    database = await client.CreateDatabaseIfNotExistsAsync(databaseId);
    ContainerProperties containerProperties = new (containerId, partitionKeyPath: "/partition_key");
    container = await database.CreateContainerIfNotExistsAsync(containerProperties);
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/heartbeat", () => Results.Ok("Cosmos DB Linux API"));

app.MapPost("/item", async ([FromBody] Item item) =>
{
    var response = await container.CreateItemAsync(item, new PartitionKey(item.partition_key));
    return response.StatusCode == HttpStatusCode.Created ? Results.Ok(item.Id) : Results.BadRequest(response.StatusCode);
});

app.UseHttpsRedirection();

app.Run();

internal record Item(Guid Id, string Name, double Value, DateTime ExpirationDate, string partition_key);