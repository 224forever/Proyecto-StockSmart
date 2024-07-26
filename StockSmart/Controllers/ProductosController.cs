using Microsoft.AspNetCore.Mvc;
using StockSmart.Models;
using StockSmart.Services;
using System.Threading.Tasks;

namespace StockSmart.Controllers
{
    public class ProductosController : Controller
    {
        private readonly IProductService _productService;

        public ProductosController(IProductService productService)
        {
            _productService = productService;
        }

        public async Task<IActionResult> Index(string reference, string productName, decimal? minPrice, decimal? maxPrice)
        {
            var productos = await _productService.SearchProductsAsync(reference, productName, minPrice, maxPrice);
            return View(productos);
        }

        public async Task<IActionResult> Details(string id)
        {
            var producto = await _productService.GetProductByIdAsync(id);
            if (producto == null)
            {
                return NotFound();
            }
            return View(producto);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto)
        {
            if (ModelState.IsValid)
            {
                await _productService.AddProductAsync(producto);
                return RedirectToAction(nameof(Index));
            }
            return View(producto);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var producto = await _productService.GetProductByIdAsync(id);
            if (producto == null)
            {
                return NotFound();
            }
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Producto producto)
        {
            if (id != producto.ProductID)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                await _productService.UpdateProductAsync(producto);
                return RedirectToAction(nameof(Index));
            }
            return View(producto);
        }

        public async Task<IActionResult> Delete(string id)
        {
            var producto = await _productService.GetProductByIdAsync(id);
            if (producto == null)
            {
                return NotFound();
            }
            return View(producto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _productService.DeleteProductAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}