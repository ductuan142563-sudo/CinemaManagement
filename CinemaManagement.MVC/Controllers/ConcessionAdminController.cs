using System.Text.Json;
using CinemaManagement.MVC.Helpers;
using CinemaManagement.MVC.Services;
using CinemaManagement.MVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CinemaManagement.MVC.Controllers;

[AuthorizeSession(Roles = new[] { "Admin" })]
public class ConcessionAdminController : Controller
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ApiService _api;

    public ConcessionAdminController(ApiService api)
    {
        _api = api;
    }

    // ---------- Danh sách tất cả món (kể cả món đã ẩn) ----------
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = new ConcessionAdminPageViewModel();

        try
        {
            var (ok, content) = await _api.GetRawAsync("api/concession/manage");

            // Token hết hạn: ApiService đã xóa Session -> đưa về trang đăng nhập
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (!ok)
                vm.LoadError = DescribeError(content);
            else
                vm.Items = JsonSerializer.Deserialize<List<ConcessionAdminItemViewModel>>(content, JsonOptions)
                           ?? new List<ConcessionAdminItemViewModel>();
        }
        catch (JsonException)
        {
            vm.LoadError = "API trả về dữ liệu không đọc được.";
        }
        catch (Exception)
        {
            vm.LoadError = "Không kết nối được API. Hãy kiểm tra API đang chạy.";
        }

        return View(vm);
    }

    // ---------- Thêm món ----------
    [HttpGet]
    public IActionResult Create()
    {
        return View(new ConcessionAdminItemViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ConcessionAdminItemViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var (success, message) = await _api.PostAsync("api/concession", ToRequest(model));

            if (!success)
            {
                ModelState.AddModelError("", "Thêm món thất bại: " + message);
                return View(model);
            }
        }
        catch (Exception)
        {
            ModelState.AddModelError("", "Không kết nối được API. Vui lòng thử lại.");
            return View(model);
        }

        TempData["Success"] = $"Đã thêm món \"{model.Name.Trim()}\".";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Sửa món ----------
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var item = await _api.GetAsync<ConcessionAdminItemViewModel>($"api/concession/{id}");

            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (item == null)
            {
                TempData["Error"] = "Không tìm thấy món.";
                return RedirectToAction(nameof(Index));
            }

            return View(item);
        }
        catch (Exception)
        {
            TempData["Error"] = "Không kết nối được API. Vui lòng thử lại.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ConcessionAdminItemViewModel model)
    {
        if (model.ConcessionItemID <= 0)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var (success, message) = await _api.PutAsync($"api/concession/{model.ConcessionItemID}", ToRequest(model));

            if (!success)
            {
                ModelState.AddModelError("", "Cập nhật thất bại: " + message);
                return View(model);
            }
        }
        catch (Exception)
        {
            ModelState.AddModelError("", "Không kết nối được API. Vui lòng thử lại.");
            return View(model);
        }

        TempData["Success"] = $"Đã cập nhật món \"{model.Name.Trim()}\".";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Ẩn hoặc hiện lại món ----------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool isActive)
    {
        try
        {
            var (success, message) = await _api.PutAsync($"api/concession/{id}/active", new { isActive });

            if (success)
                TempData["Success"] = isActive ? "Đã hiện lại món trên thực đơn." : "Đã ẩn món khỏi thực đơn.";
            else
                TempData["Error"] = "Thao tác thất bại: " + message;
        }
        catch (Exception)
        {
            TempData["Error"] = "Không kết nối được API. Vui lòng thử lại.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ---------- Hàm phụ ----------

    private static object ToRequest(ConcessionAdminItemViewModel model) => new
    {
        name = model.Name.Trim(),
        description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
        category = model.Category,
        price = model.Price,
        isActive = model.IsActive
    };

    // Chuyển nội dung lỗi của API thành câu dễ hiểu (chỉ Admin thấy trang này)
    private static string DescribeError(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "API từ chối hoặc không tìm thấy chức năng này. Hãy chắc chắn API đã chạy bản mới " +
                   "(có nhóm Concession trong Swagger) rồi đăng nhập lại bằng tài khoản Admin.";

        var text = content.Trim();
        return text.Length > 300 ? text[..300] + "..." : text;
    }
}