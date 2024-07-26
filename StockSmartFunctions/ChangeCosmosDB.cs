using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace StockSmartFunctions
{
    public class ChangeCosmosDB
    {
        private readonly ILogger _logger;
        private static CloudTableClient _tableClient;
        private static CloudTable _logTable;

        public ChangeCosmosDB(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ChangeCosmosDB>();
            InitializeStorage();
        }

        private static void InitializeStorage()
        {
            // Recuperar la URI del Key Vault desde las variables de entorno
            var keyVaultUri = Environment.GetEnvironmentVariable("KeyVaultUri");
            var secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());

            // Obtener la cadena de conexión del almacenamiento desde el Key Vault
            var storageConnectionString = secretClient.GetSecret("StorageAccountConnectionString").Value.Value;

            // Inicializar el cliente y la tabla de almacenamiento
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            _tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            _logTable = _tableClient.GetTableReference("OperationLogs");
            _logTable.CreateIfNotExistsAsync().Wait();
        }

        [Function("ChangeCosmosDB")]
        public async Task Run(
            [CosmosDBTrigger(
                databaseName: "YourDatabaseName",
                containerName: "YourContainerName",
                Connection = "CosmosDBConnectionString",
                LeaseContainerName = "leases",
                CreateLeaseContainerIfNotExists = true)] IReadOnlyList<MyInfo> input)
        {
            if (input != null && input.Count > 0)
            {
                _logger.LogInformation($"Documents modified: {input.Count}");
                _logger.LogInformation($"First document Id: {input[0].id}");

                foreach (var document in input)
                {
                    var logEntry = new LogEntity(document.id)
                    {
                        OperationType = "Update",
                        Timestamp = DateTime.UtcNow,
                        Data = $"Text: {document.Text}, Number: {document.Number}, Boolean: {document.Boolean}"
                    };

                    var insertOperation = TableOperation.Insert(logEntry);
                    await _logTable.ExecuteAsync(insertOperation);
                }
            }
        }
    }

    public class MyInfo
    {
        public string id { get; set; }
        public string Text { get; set; }
        public int Number { get; set; }
        public bool Boolean { get; set; }
    }

    public class LogEntity : TableEntity
    {
        public LogEntity(string documentId)
        {
            PartitionKey = "CosmosDBUpdates";
            RowKey = documentId;
        }

        public string OperationType { get; set; }
        public DateTime Timestamp { get; set; }
        public string Data { get; set; }
    }
}
