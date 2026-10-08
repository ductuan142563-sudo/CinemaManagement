using CinemaManagement.MVC.Helpers;
using CinemaManagement.MVC.Services;
using CinemaManagement.MVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CinemaManagement.MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApiService _api;

        public AccountController(ApiService api)
        {
            _api = api;
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

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                // Tùy response API của bạn – chỉnh key cho khớp
                int userId = root.TryGetProperty("userId", out var uid) ? uid.GetInt32()
                           : root.TryGetProperty("UserID", out var uid2) ? uid2.GetInt32() : 0;

                string username = root.TryGetProperty("username", out var un) ? un.GetString() ?? model.Username
                                : root.TryGetProperty("Username", out var un2) ? un2.GetString() ?? model.Username
                                : model.Username;

                string fullName = root.TryGetProperty("fullName", out var fn) ? fn.GetString() ?? ""
                                : root.TryGetProperty("FullName", out var fn2) ? fn2.GetString() ?? ""
                                : "";

                string role = root.TryGetProperty("role", out var r) ? r.GetString() ?? "Member"
                            : root.TryGetProperty("Role", out var r2) ? r2.GetString() ?? "Member"
                            : "Member";

                int? memberId = null;
                if (root.TryGetProperty("memberId", out var mid) && mid.ValueKind != JsonValueKind.Null)
                    memberId = mid.GetInt32();
                else if (root.TryGetProperty("MemberID", out var mid2) && mid2.ValueKind != JsonValueKind.Null)
                    memberId = mid2.GetInt32();

                SessionHelper.SetUser(HttpContext.Session, userId, username, fullName, role, memberId);
            }
            catch
            {
                // Nếu API chỉ trả message thành công, lưu tối thiểu
                SessionHelper.SetUser(HttpContext.Session, 0, model.Username, model.Username, "Member");
            }

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
            return RedirectToAction("Login");
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}