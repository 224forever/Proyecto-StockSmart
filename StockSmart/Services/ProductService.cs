using Azure.Storage.Blobs;
using Newtonsoft.Json.Linq;
using StockSmart.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StockSmart.Services
{
    public class ProductService : IProductService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private const string ContainerName = "data";
        private const string BlobName = "products.json";

        public ProductService(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        public async Task<List<Producto>> GetProductsAsync()
        {
            var jArray = await GetProductsJArrayAsync();
            return jArray.ToObject<List<Producto>>();
        }

        public async Task<Producto> GetProductByIdAsync(string id)
        {
            var products = await GetProductsAsync();
            return products.FirstOrDefault(p => p.ProductID == id);
        }

        public async Task<List<Producto>> SearchProductsAsync(string reference, string productName, decimal? minPrice, decimal? maxPrice)
        {
            var products = await GetProductsAsync();
            return products.Where(p =>
                (string.IsNullOrEmpty(reference) || p.ProductID.Contains(reference)) &&
                (string.IsNullOrEmpty(productName) || p.ProductName.Contains(productName)) &&
                (!minPrice.HasValue || decimal.Parse(p.UnitPrice) >= minPrice.Value) &&
                (!maxPrice.HasValue || decimal.Parse(p.UnitPrice) <= maxPrice.Value)
            ).ToList();
        }

        public async Task AddProductAsync(Producto product)
        {
            var jArray = await GetProductsJArrayAsync();
            jArray.Add(JObject.FromObject(product));
            await SaveProductsJArrayAsync(jArray);
        }

        public async Task UpdateProductAsync(Producto product)
        {
            var jArray = await GetProductsJArrayAsync();
            var existingProduct = jArray.FirstOrDefault(p => p["ProductID"].ToString() == product.ProductID);
            if (existingProduct != null)
            {
                existingProduct.Replace(JObject.FromObject(product));
                await SaveProductsJArrayAsync(jArray);
            }
        }

        public async Task DeleteProductAsync(string id)
        {
            var jArray = await GetProductsJArrayAsync();
            var productToRemove = jArray.FirstOrDefault(p => p["ProductID"].ToString() == id);
            if (productToRemove != null)
            {
                jArray.Remove(productToRemove);
                await SaveProductsJArrayAsync(jArray);
            }
        }

        private async Task<JArray> GetProductsJArrayAsync()
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            var blobClient = containerClient.GetBlobClient(BlobName);

            if (await blobClient.ExistsAsync())
            {
                var response = await blobClient.DownloadAsync();
                using (var reader = new StreamReader(response.Value.Content))
                {
                    var content = await reader.ReadToEndAsync();
                    return JArray.Parse(content);
                }
            }
            else
            {
                throw new FileNotFoundException("El archivo products.json no existe en el contenedor.");
            }
        }

        private async Task SaveProductsJArrayAsync(JArray jArray)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            var blobClient = containerClient.GetBlobClient(BlobName);

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(jArray.ToString());
                writer.Flush();
                stream.Position = 0;
                await blobClient.UploadAsync(stream, overwrite: true);
            }
        }
    }
}