using Microsoft.AspNetCore.Mvc;

namespace StockSmart.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
