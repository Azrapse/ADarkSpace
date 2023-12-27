using ADSCommon.Data;
using ADSCommon.Entities;
using ADSFront.Models;
using ADSFront.Util;
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
            ViewBag.Message = TempData["Message"];
            var loginModel = new LoginViewModel();
            return View(loginModel);
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var loginUrl = @$"http://{Environment.GetEnvironmentVariable("LOGINWORKER_HOST")}/Login/";
            // Use HttpClient to send an http web request to the worker url to obtain the JSON data.
            var client = new HttpClient();
            var loginData = new LoginData(model.UserName, model.Password);
            var response = await client.PostAsync(loginUrl, JsonContent.Create(loginData));
            if (response.IsSuccessStatusCode)
            {
                var player = await response.Content.ReadFromJsonAsync<Player>();
                HttpContext.Session.Set("Player", player);
                return RedirectToAction("Play", "Home");
            }
            else
            {                
                TempData["Message"] = await response.Content.ReadAsStringAsync();
                return RedirectToAction("Login", "Home");
            }
        }
               

        public IActionResult Register()
        {
            ViewBag.Message = TempData["Message"];
            var registerModel = new RegisterViewModel();
            return View(registerModel);
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            var registerUrl = @$"http://{Environment.GetEnvironmentVariable("LOGINWORKER_HOST")}/Register/";
            // Use HttpClient to send an http web request to the worker url to obtain the JSON data.
            var client = new HttpClient();
            var registerData = new RegisterData(model.UserName, model.Password, model.Name, model.EMail, model.ConfirmPassword);
            var response = await client.PostAsync(registerUrl, JsonContent.Create(registerData));
            if (response.IsSuccessStatusCode)
            {
                var player = await response.Content.ReadFromJsonAsync<Player>();
                HttpContext.Session.Set("Player", player);
                return RedirectToAction("Play", "Home");
            }
            else
            {
                TempData["Message"] = await response.Content.ReadAsStringAsync();
                return RedirectToAction("Register", "Home");
            }            
        }

        public IActionResult Play()
        {
            var player = HttpContext.Session.Get<Player>("Player");
            if (player is null)
            {
                TempData["Message"] = "You must login first";
                return RedirectToAction("Login", "Home");
            }
            return View(player);
        }
    }
}