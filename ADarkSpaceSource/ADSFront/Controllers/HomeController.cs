using ADSFront.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;

namespace ADSFront.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Login()
        {
            var loginModel = new LoginViewModel();
            return View(loginModel);
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            // ToDo: Perform the login
            return RedirectToAction("Index", "Home");
        }
               

        public IActionResult Register()
        {
            var registerModel = new RegisterViewModel();
            return View(registerModel);
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            // ToDo: Do the registering.
            return RedirectToAction("Index", "Home");
        }

    }
}