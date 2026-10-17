using CinemaManagement.MVC.Helpers;
using CinemaManagement.MVC.Services;
using CinemaManagement.MVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CinemaManagement.MVC.Controllers;

[AuthorizeSession]
public class ConcessionController : Controller
{
    private const int MaxPerItem = 10;
    private const int MaxNoteLength = 200;

    private readonly ApiService _api;

    public ConcessionController(ApiService api)
    {
        _api = api;
    }

    // ---------- Thực đơn bắp nước ----------
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = new ConcessionMenuViewModel();

        try
        {
            var items = await _api.GetAsync<List<ConcessionItemViewModel>>("api/concession");

            // Token hết hạn: ApiService đã xóa Session -> đưa về trang đăng nhập
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (items == null)
                vm.LoadFailed = true;
            else
                vm.Items = items;
        }
        catch (Exception)
        {
            vm.LoadFailed = true;
        }

        return View(vm);
    }

    // ---------- Đặt bắp nước ----------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Dictionary<int, int>? quantities, string? note)
    {
        var lines = (quantities ?? new Dictionary<int, int>())
            .Where(q => q.Value > 0)
            .ToList();

        if (lines.Count == 0)
        {
            TempData["Error"] = "Vui lòng chọn ít nhất một món.";
            return RedirectToAction(nameof(Index));
        }

        if (lines.Any(l => l.Value > MaxPerItem))
        {
            TempData["Error"] = $"Mỗi món đặt tối đa {MaxPerItem} phần.";
            return RedirectToAction(nameof(Index));
        }

        if (!string.IsNullOrWhiteSpace(note) && note.Trim().Length > MaxNoteLength)
        {
            TempData["Error"] = $"Ghi chú tối đa {MaxNoteLength} ký tự.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // PostAsync trả về tuple (Success, Data, Message)
            var result = await _api.PostAsync<FoodOrderResultViewModel>("api/concession/orders", new
            {
                items = lines.Select(l => new { concessionItemID = l.Key, quantity = l.Value }),
                note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
            });

            var data = result.Data;

            if (result.Success && data != null && data.Success)
            {
                TempData["Success"] =
                    $"Đặt bắp nước thành công! Mã đơn: {data.OrderCode}. Đưa mã này tại quầy bắp nước để nhận món.";
                return RedirectToAction(nameof(Orders));
            }

            // Thất bại: ưu tiên thông báo của API
            TempData["Error"] = !string.IsNullOrWhiteSpace(data?.Message) ? data!.Message
                              : !string.IsNullOrWhiteSpace(result.Message) ? result.Message
                              : "Đặt bắp nước thất bại.";
        }
        catch (Exception)
        {
            TempData["Error"] = "Có lỗi xảy ra, vui lòng thử lại.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ---------- Đơn của tôi ----------
    [HttpGet]
    public async Task<IActionResult> Orders()
    {
        var vm = new FoodOrdersPageViewModel();

        try
        {
            var orders = await _api.GetAsync<List<FoodOrderViewModel>>("api/concession/orders/mine");

            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (orders == null)
                vm.LoadFailed = true;
            else
                vm.Orders = orders;
        }
        catch (Exception)
        {
            vm.LoadFailed = true;
        }

        return View(vm);
    }
}