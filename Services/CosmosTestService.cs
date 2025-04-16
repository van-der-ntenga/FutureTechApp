using Microsoft.Azure.Cosmos;
using System;
using System.Threading.Tasks;

namespace FutureTechApp.Services
{
    public class CosmosTestService
    {
        //private readonly CosmosClient _cosmosClient;

        //public CosmosTestService(CosmosClient cosmosClient)
        //{
        //    _cosmosClient = cosmosClient;
        //}

        //public async Task RunTestAsync()
        //{
        //    string databaseName = "TestDb";
        //    string containerName = "TestContainer";
        //    string partitionKeyPath = "/id";

        //    try
        //    {
        //        // 1. Create database
        //        DatabaseResponse databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
        //        Console.WriteLine($"Database '{databaseResponse.Database.Id}' created or already exists.");

        //        // 2. Create container
        //        ContainerResponse containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(containerName, partitionKeyPath);
        //        Console.WriteLine($"Container '{containerResponse.Container.Id}' created or already exists.");

        //        // 3. Insert sample item
        //        var item = new
        //        {
        //            id = Guid.NewGuid().ToString(),
        //            message = "Hello Cosmos DB!"
        //        };

        //        var response = await containerResponse.Container.CreateItemAsync(item, new PartitionKey(item.id));
        //        Console.WriteLine($"Item inserted with id: {item.id}");
        //    }
        //    catch (CosmosException ex)
        //    {
        //        Console.WriteLine($"CosmosException: {ex.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Exception: {ex.Message}");
        //    }
        //}
    }
}
