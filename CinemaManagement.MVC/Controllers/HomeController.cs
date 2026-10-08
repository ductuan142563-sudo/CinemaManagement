using CinemaManagement.MVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaManagement.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiService _api;

        public HomeController(ApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            // Test gọi API
            var (success, content) = await _api.GetRawAsync("api/report/summary");

            ViewBag.ApiTest = success
                ? $"API OK: {content}"
                : $"API lỗi: {content}";

            return View();
        }
    }
}