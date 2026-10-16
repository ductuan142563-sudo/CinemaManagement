using CinemaManagement.MVC.Helpers;
using CinemaManagement.MVC.Hubs;
using CinemaManagement.MVC.Services;
using CinemaManagement.MVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CinemaManagement.MVC.Controllers;

[AuthorizeSession]
public class BookingController : Controller
{
    private readonly ApiService _api;
    private readonly IConfiguration _config;
    private readonly IHubContext<SeatHub> _hub;

    public BookingController(ApiService api, IConfiguration config, IHubContext<SeatHub> hub)
    {
        _api = api;
        _config = config;
        _hub = hub;
    }

    // ---------- Bước 1: lưới poster các phim đang chiếu ----------
    public async Task<IActionResult> Index()
    {
        var vm = new BookingIndexViewModel();

        try
        {
            var movies = await _api.GetAsync<List<BookingMovieDto>>("api/movie");
            var showtimes = await _api.GetAsync<List<BookingShowtimeDto>>("api/showtime");

            // Token hết hạn: ApiService đã xóa Session -> đưa về trang đăng nhập
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (movies == null)
            {
                TempData["Error"] = "Không tải được danh sách phim. Vui lòng thử lại.";
                return View(vm);
            }

            var now = DateTime.Now;
            var movieIdsWithShowtimes = (showtimes ?? new List<BookingShowtimeDto>())
                .Where(s => s.IsActive && s.StartTime > now)
                .Select(s => s.MovieID)
                .ToHashSet();

            vm.Items = movies
                .Where(m => m.IsActive)
                .Select(m => new BookingMovieCard
                {
                    Movie = m,
                    HasShowtimes = movieIdsWithShowtimes.Contains(m.MovieID)
                })
                .OrderByDescending(c => c.HasShowtimes)
                .ThenBy(c => c.Movie.Title)
                .ToList();
        }
        catch (Exception)
        {
            TempData["Error"] = "Không tải được danh sách phim. Vui lòng thử lại.";
        }

        return View(vm);
    }

    // ---------- Bước 2: trang riêng của một phim, chọn suất chiếu ----------
    [HttpGet]
    public async Task<IActionResult> Movie(int id)
    {
        try
        {
            var movie = await _api.GetAsync<BookingMovieDto>($"api/movie/{id}");
            var showtimes = await _api.GetAsync<List<BookingShowtimeDto>>($"api/showtime?movieId={id}");

            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (movie == null || !movie.IsActive)
            {
                TempData["Error"] = "Không tìm thấy phim.";
                return RedirectToAction(nameof(Index));
            }

            var now = DateTime.Now;

            var vm = new BookingMoviePageViewModel
            {
                Movie = movie,
                Showtimes = (showtimes ?? new List<BookingShowtimeDto>())
                    .Where(s => s.MovieID == id && s.IsActive && s.StartTime > now)
                    .OrderBy(s => s.StartTime)
                    .ToList()
            };

            return View(vm);
        }
        catch (Exception)
        {
            TempData["Error"] = "Không tải được thông tin phim. Vui lòng thử lại.";
            return RedirectToAction(nameof(Index));
        }
    }

    // ---------- Bước 3: trang sơ đồ ghế ----------
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

    // ---------- Bước 4: tạo booking ----------
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
            {
                // Báo ngay cho mọi người đang xem suất chiếu này
                await BroadcastSeatsBookedAsync(request.ShowtimeId, request.SeatIds);
                return Json(new { success = true, data });
            }

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

    // ---------- Hàm phụ ----------

    // Đọc MemberId từ Session (chịu được cả SetString lẫn SetInt32)
    private int? GetSessionMemberId() => HttpContext.Session.GetMemberIdOrNull();

    // Gửi danh sách ghế vừa đặt cho mọi trình duyệt đang xem suất chiếu này.
    // Lỗi ở bước này không được làm hỏng việc đặt vé đã thành công.
    private async Task BroadcastSeatsBookedAsync(int showtimeId, List<int> seatIds)
    {
        try
        {
            await _hub.Clients.Group(SeatHub.GroupName(showtimeId))
                .SendAsync("SeatsBooked", new { showtimeId, seatIds });
        }
        catch (Exception)
        {
            // Các trình duyệt vẫn tự cập nhật khi hỏi lại máy chủ định kỳ
        }
    }
}