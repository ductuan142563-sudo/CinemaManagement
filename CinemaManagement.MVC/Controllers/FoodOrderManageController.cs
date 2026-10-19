using System.Text.Json;
using CinemaManagement.MVC.Helpers;
using CinemaManagement.MVC.Services;
using CinemaManagement.MVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CinemaManagement.MVC.Controllers;

[AuthorizeSession(Roles = new[] { "Admin", "Staff" })]
public class FoodOrderManageController : Controller
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ApiService _api;

    public FoodOrderManageController(ApiService api)
    {
        _api = api;
    }

    // ---------- Danh sách đơn cần xử lý và đơn gần đây ----------
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = new FoodOrderManagePageViewModel();

        try
        {
            var (ok, content) = await _api.GetRawAsync("api/concession/orders/manage");

            // Token hết hạn: ApiService đã xóa Session -> đưa về trang đăng nhập
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (!ok)
            {
                vm.LoadError = DescribeError(content);
            }
            else
            {
                var all = JsonSerializer.Deserialize<List<FoodOrderManageViewModel>>(content, JsonOptions)
                          ?? new List<FoodOrderManageViewModel>();

                vm.Open = all.Where(o => o.Status == "Confirmed").OrderBy(o => o.CreatedAt).ToList();
                vm.Done = all.Where(o => o.Status != "Confirmed").Take(30).ToList();
            }
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

    // ---------- Xác nhận đã nhận tiền (chuyển khoản QR hoặc tiền mặt) ----------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ConfirmPayment(string code) => Act(code, "confirm-payment");

    // ---------- Giao món (COD: ghi nhận thu tiền mặt luôn) ----------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Complete(string code) => Act(code, "complete");

    // ---------- Hủy đơn ----------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Cancel(string code) => Act(code, "cancel");

    // ---------- Hàm phụ ----------

    private async Task<IActionResult> Act(string code, string action)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            TempData["Error"] = "Thiếu mã đơn.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var (success, message) = await _api.PutAsync($"api/concession/orders/{Uri.EscapeDataString(code)}/{action}");

            if (success)
                TempData["Success"] = $"{code}: {ReadMessage(message)}";
            else
                TempData["Error"] = $"{code}: {message}";
        }
        catch (Exception)
        {
            TempData["Error"] = "Không kết nối được API. Vui lòng thử lại.";
        }

        return RedirectToAction(nameof(Index));
    }

    // Lấy riêng phần "message" từ nội dung JSON thành công của API
    private static string ReadMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("message", out var msg) &&
                msg.ValueKind == JsonValueKind.String)
            {
                return msg.GetString() ?? "Thành công.";
            }
        }
        catch (JsonException)
        {
        }

        return "Thành công.";
    }

    private static string DescribeError(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "API từ chối hoặc không tìm thấy chức năng này. Hãy chắc chắn API đã chạy bản mới " +
                   "(có nhóm Concession trong Swagger) rồi đăng nhập lại.";

        var text = content.Trim();
        return text.Length > 300 ? text[..300] + "..." : text;
    }
}