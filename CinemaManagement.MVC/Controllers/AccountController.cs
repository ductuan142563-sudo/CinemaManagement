using CinemaManagement.MVC.Helpers;
using CinemaManagement.MVC.Services;
using CinemaManagement.MVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CinemaManagement.MVC.Controllers
{
    public class AccountController : Controller
    {
        private const string TokenKey = "Token";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ApiService _api;

        public AccountController(ApiService api)
        {
            _api = api;
        }

        // Khớp response của POST api/auth/login (không phân biệt hoa thường: userID/userId...)
        private class LoginApiResponse
        {
            public int UserID { get; set; }
            public string? Username { get; set; }
            public string? FullName { get; set; }
            public string? Role { get; set; }
            public int? MemberID { get; set; }
            public string? Token { get; set; }
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var (success, content) = await _api.PostAsync("api/auth/login", new
            {
                username = model.Username,
                password = model.Password
            });

            if (!success)
            {
                ModelState.AddModelError("", "Username hoặc Password không đúng.");
                return View(model);
            }

            LoginApiResponse? data = null;
            try
            {
                data = JsonSerializer.Deserialize<LoginApiResponse>(content, JsonOptions);
            }
            catch (JsonException)
            {
                // xử lý ở bên dưới
            }

            if (data == null || string.IsNullOrWhiteSpace(data.Token))
            {
                ModelState.AddModelError("", "Đăng nhập thất bại: không nhận được phiên đăng nhập từ máy chủ.");
                return View(model);
            }

            var username = string.IsNullOrWhiteSpace(data.Username) ? model.Username : data.Username;
            var fullName = string.IsNullOrWhiteSpace(data.FullName) ? username : data.FullName;
            var role = string.IsNullOrWhiteSpace(data.Role) ? "Member" : data.Role;

            SessionHelper.SetUser(HttpContext.Session, data.UserID, username, fullName, role, data.MemberID);
            HttpContext.Session.SetString(TokenKey, data.Token);

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var (success, content) = await _api.PostAsync("api/member/register", new
            {
                username = model.Username,
                password = model.Password,
                fullName = model.FullName,
                email = model.Email,
                phone = model.Phone
            });

            if (!success)
            {
                ModelState.AddModelError("", "Đăng ký thất bại. Username có thể đã tồn tại.");
                return View(model);
            }

            TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            SessionHelper.Clear(HttpContext.Session);
            HttpContext.Session.Remove(TokenKey);
            return RedirectToAction("Login");
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}