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
    private readonly IConfiguration _config;

    public ConcessionController(ApiService api, IConfiguration config)
    {
        _api = api;
        _config = config;
    }

    // ---------- Thực đơn bắp nước ----------
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = new ConcessionMenuViewModel
        {
            QrEnabled = PaymentQrHelper.IsConfigured(_config)
        };

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
    public async Task<IActionResult> Create(Dictionary<int, int>? quantities, string? note, string? paymentMethod)
    {
        var method = paymentMethod?.Trim().ToUpperInvariant();
        if (method != "COD" && method != "QR")
        {
            TempData["Error"] = "Vui lòng chọn phương thức thanh toán.";
            return RedirectToAction(nameof(Index));
        }

        if (method == "QR" && !PaymentQrHelper.IsConfigured(_config))
        {
            TempData["Error"] = "Thanh toán QR chưa được cấu hình. Vui lòng chọn tiền mặt hoặc liên hệ quản trị viên.";
            return RedirectToAction(nameof(Index));
        }

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
                note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                paymentMethod = method
            });

            var data = result.Data;

            if (result.Success && data != null && data.Success && !string.IsNullOrWhiteSpace(data.OrderCode))
            {
                TempData["Success"] = method == "QR"
                    ? "Đặt bắp nước thành công! Quét mã QR bên dưới để thanh toán."
                    : "Đặt bắp nước thành công! Thanh toán tiền mặt khi nhận món tại quầy.";

                return RedirectToAction(nameof(Details), new { code = data.OrderCode });
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

    // ---------- Chi tiết một đơn (mã QR nếu thanh toán QR và chưa trả) ----------
    [HttpGet]
    public async Task<IActionResult> Details(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return RedirectToAction(nameof(Orders));

        try
        {
            var order = await _api.GetAsync<FoodOrderViewModel>($"api/concession/orders/{Uri.EscapeDataString(code)}");

            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn.";
                return RedirectToAction(nameof(Orders));
            }

            var vm = new FoodOrderDetailsViewModel { Order = order };

            if (order.PaymentMethod == "QR" && order.PaymentStatus != "Paid" && order.Status != "Cancelled")
                vm.Qr = PaymentQrHelper.Build(_config, order.OrderCode, order.TotalAmount);

            return View(vm);
        }
        catch (Exception)
        {
            TempData["Error"] = "Không tải được thông tin đơn. Vui lòng thử lại.";
            return RedirectToAction(nameof(Orders));
        }
    }

    // ---------- Trạng thái đơn (trang chi tiết hỏi lại mỗi vài giây khi chờ thanh toán QR) ----------
    [HttpGet]
    public async Task<IActionResult> Status(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Json(new { found = false });

        try
        {
            var order = await _api.GetAsync<FoodOrderViewModel>($"api/concession/orders/{Uri.EscapeDataString(code)}");

            if (order == null)
                return Json(new { found = false });

            return Json(new { found = true, paymentStatus = order.PaymentStatus, status = order.Status });
        }
        catch (Exception)
        {
            return Json(new { found = false });
        }
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