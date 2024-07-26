using System.Collections.Generic;
using System.Threading.Tasks;
using StockSmart.Models;

namespace StockSmart.Services
{
    public interface IProductService
    {
        Task<List<Producto>> GetProductsAsync();
        Task<Producto> GetProductByIdAsync(string id);
        Task<List<Producto>> SearchProductsAsync(string reference, string productName, decimal? minPrice, decimal? maxPrice);
        Task AddProductAsync(Producto product);
        Task UpdateProductAsync(Producto product);
        Task DeleteProductAsync(string id);
    }
}