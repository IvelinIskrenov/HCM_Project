using System.Diagnostics;
using HCM_Project.Models;
using Microsoft.AspNetCore.Mvc;

namespace HCM_Project.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        // Constructor: injects logger service
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // GET: /
        // Default home page
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Home/Privacy
        // Static "Privacy Policy" page
        public IActionResult Privacy()
        {
            return View();
        }

        // GET: /Home/Error
        // Error handling page. Displays error info including RequestId.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}

