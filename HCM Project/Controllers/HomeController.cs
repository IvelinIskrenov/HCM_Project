using System.Diagnostics;
using HCM_Project.Models;
using Microsoft.AspNetCore.Mvc;

namespace HCM_Project.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        //GET: default home page
        public IActionResult Index()
        {
            return View();
        }


        //GET:- "Privacy Policy" page
        public IActionResult Privacy()
        {
            return View();
        }

        //GET: - error handling page. 
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

