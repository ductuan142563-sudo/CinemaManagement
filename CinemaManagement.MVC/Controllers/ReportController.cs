using System.Globalization;
using CinemaManagement.MVC.Helpers;   // chỉnh namespace nếu [AuthorizeSession] / SessionHelper nằm chỗ khác
using CinemaManagement.MVC.Services;
using CinemaManagement.MVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CinemaManagement.MVC.Controllers;

[AuthorizeSession]
public class ReportController : Controller
{
    private const int MaxRangeDays = 366;

    private readonly ApiService _api;

    public ReportController(ApiService api)
    {
        _api = api;
    }

    // ---------- Dashboard: các thẻ tổng quan ----------
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsAdmin()) return DenyAccess();

        var vm = new ReportSummaryViewModel();
        try
        {
            vm = await _api.GetAsync<ReportSummaryViewModel>("api/report/summary") ?? new();
        }
        catch (Exception)
        {
            TempData["Error"] = "Không tải được số liệu tổng quan. Vui lòng thử lại.";
        }

        return View(vm);
    }

    // ---------- Doanh thu theo ngày (bảng + biểu đồ) ----------
    [HttpGet]
    public async Task<IActionResult> Revenue(DateTime? fromDate, DateTime? toDate)
    {
        if (!IsAdmin()) return DenyAccess();

        var (from, to) = NormalizeRange(fromDate, toDate);
        var vm = new RevenueReportViewModel { FromDate = from, ToDate = to };

        var rangeError = ValidateRange(from, to);
        if (rangeError != null)
        {
            TempData["Error"] = rangeError;
            return View(vm);
        }

        try
        {
            var items = await _api.GetAsync<List<DailyRevenueViewModel>>(
                $"api/report/revenue?{BuildDateQuery(from, to)}") ?? new();
            vm.Items = items.OrderBy(i => i.Date).ToList();
        }
        catch (Exception)
        {
            TempData["Error"] = "Không tải được báo cáo doanh thu. Vui lòng thử lại.";
        }

        return View(vm);
    }

    // ---------- Phim bán chạy ----------
    [HttpGet]
    public async Task<IActionResult> TopMovies(DateTime? fromDate, DateTime? toDate, int top = 5)
    {
        if (!IsAdmin()) return DenyAccess();

        var (from, to) = NormalizeRange(fromDate, toDate);
        if (top < 1 || top > 20) top = 5;

        var vm = new TopMoviesReportViewModel { FromDate = from, ToDate = to, Top = top };

        var rangeError = ValidateRange(from, to);
        if (rangeError != null)
        {
            TempData["Error"] = rangeError;
            return View(vm);
        }

        try
        {
            vm.Items = await _api.GetAsync<List<TopMovieViewModel>>(
                $"api/report/top-movies?{BuildDateQuery(from, to)}&top={top}") ?? new();
        }
        catch (Exception)
        {
            TempData["Error"] = "Không tải được báo cáo phim bán chạy. Vui lòng thử lại.";
        }

        return View(vm);
    }

    // ---------- Hàm phụ ----------

    private bool IsAdmin() => SessionHelper.GetRole(HttpContext.Session) == "Admin";

    private IActionResult DenyAccess()
    {
        TempData["Error"] = "Bạn không có quyền truy cập trang báo cáo.";
        return RedirectToAction("Index", "Home");
    }

    // Mặc định: 30 ngày gần nhất
    private static (DateTime From, DateTime To) NormalizeRange(DateTime? from, DateTime? to)
    {
        var today = DateTime.Today;
        var f = (from ?? today.AddDays(-29)).Date;
        var t = (to ?? today).Date;
        return (f, t);
    }

    private static string? ValidateRange(DateTime from, DateTime to)
    {
        if (from > to)
            return "Ngày bắt đầu không được lớn hơn ngày kết thúc.";
        if ((to - from).TotalDays > MaxRangeDays)
            return $"Khoảng ngày tối đa là {MaxRangeDays} ngày.";
        return null;
    }

    // fromDate = 00:00 của ngày bắt đầu, toDate = 23:59:59 của ngày kết thúc (gửi dạng ISO, tránh nhầm ngày/tháng)
    private static string BuildDateQuery(DateTime from, DateTime to)
    {
        var f = from.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var t = to.Date.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
        return $"fromDate={Uri.EscapeDataString(f)}&toDate={Uri.EscapeDataString(t)}";
    }
}