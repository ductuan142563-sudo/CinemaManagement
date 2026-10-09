using CinemaManagement.MVC.Helpers;   // chỉnh namespace nếu [AuthorizeSession] của PART 2 nằm chỗ khác
using CinemaManagement.MVC.Services;
using CinemaManagement.MVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CinemaManagement.MVC.Controllers;

[AuthorizeSession]
public class BookingController : Controller
{
    private readonly ApiService _api;
    private readonly IConfiguration _config;

    public BookingController(ApiService api, IConfiguration config)
    {
        _api = api;
        _config = config;
    }

    // ---------- Bước 1: chọn phim -> load suất chiếu ----------
    public async Task<IActionResult> Index()
    {
        try
        {
            var movies = await _api.GetAsync<List<MovieOptionViewModel>>("api/movie") ?? new();
            return View(movies);
        }
        catch (Exception)
        {
            TempData["Error"] = "Không tải được danh sách phim. Vui lòng thử lại.";
            return View(new List<MovieOptionViewModel>());
        }
    }

    // AJAX: suất chiếu sắp tới của 1 phim
    [HttpGet]
    public async Task<IActionResult> Showtimes(int movieId)
    {
        try
        {
            var all = await _api.GetAsync<List<ShowtimeOptionViewModel>>("api/showtime") ?? new();
            var list = all.Where(s => s.MovieId == movieId && s.StartTime > DateTime.Now)
                          .OrderBy(s => s.StartTime)
                          .ToList();
            return Json(new { success = true, data = list });
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "Không tải được suất chiếu." });
        }
    }

    // ---------- Bước 2: trang sơ đồ ghế ----------
    [HttpGet]
    public async Task<IActionResult> SeatMap(int showtimeId)
    {
        var vm = new BookingPageViewModel
        {
            ShowtimeId = showtimeId,
            VndPerPoint = _config.GetValue("Booking:VndPerPoint", 10000)
        };

        var role = HttpContext.Session.GetString("Role");
        var memberId = GetSessionMemberId();

        if (role == "Member" && memberId.HasValue)
        {
            vm.IsMember = true;
            try
            {
                // Hồ sơ thành viên (endpoint của PART 5 phía API)
                vm.Member = await _api.GetAsync<MemberInfoViewModel>($"api/member/profile/{memberId}");
            }
            catch (Exception)
            {
                // Không chặn đặt vé nếu không lấy được hồ sơ
                TempData["Error"] = "Không tải được thông tin hạng thành viên.";
            }
        }

        return View(vm);
    }

    // AJAX: dữ liệu sơ đồ ghế
    [HttpGet]
    public async Task<IActionResult> SeatMapData(int showtimeId)
    {
        try
        {
            var map = await _api.GetAsync<SeatMapViewModel>($"api/booking/seatmap/{showtimeId}");
            if (map == null) return Json(new { success = false, message = "Không tìm thấy suất chiếu." });
            return Json(new { success = true, data = map });
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "Không tải được sơ đồ ghế." });
        }
    }

    // ---------- Bước 3: tạo booking ----------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request)
    {
        if (request == null || !ModelState.IsValid || request.SeatIds.Count == 0)
            return Json(new { success = false, message = "Vui lòng chọn ít nhất 1 ghế." });

        // Không tin MemberId từ client: Member luôn dùng MemberId trong Session
        var role = HttpContext.Session.GetString("Role");
        request.MemberId = role == "Member" ? GetSessionMemberId() : null;

        try
        {
            // PostAsync trả về tuple (Success, Data, Message)
            var result = await _api.PostAsync<BookingResultViewModel>("api/booking", request);
            var data = result.Data;

            // Thành công: API trả dữ liệu và BookingResponse.Success = true
            if (data != null && data.Success)
                return Json(new { success = true, data });

            // Thất bại: ưu tiên thông báo của API, sau đó tới thông báo của ApiService
            var message = !string.IsNullOrWhiteSpace(data?.Message) ? data!.Message
                        : !string.IsNullOrWhiteSpace(result.Message) ? result.Message
                        : "Đặt vé thất bại.";

            return Json(new
            {
                success = false,
                conflict = true,   // cho JS tải lại sơ đồ ghế (thường do ghế vừa bị đặt)
                message
            });
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "Có lỗi xảy ra, vui lòng thử lại." });
        }
    }

    // Giả định PART 2 lưu MemberId bằng SetString
    private int? GetSessionMemberId() =>
        int.TryParse(HttpContext.Session.GetString("MemberId"), out var id) ? id : null;
}