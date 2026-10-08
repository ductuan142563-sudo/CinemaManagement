using CinemaManagement.MVC.Helpers;
using CinemaManagement.MVC.Services;
using CinemaManagement.MVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CinemaManagement.MVC.Controllers
{
    [AuthorizeSession] // mọi user đã login đều đặt vé được
    public class BookingController : Controller
    {
        private readonly ApiService _api;

        public BookingController(ApiService api)
        {
            _api = api;
        }

        // GET: /Booking
        public async Task<IActionResult> Index()
        {
            var movies = await _api.GetAsync<List<MovieOption>>("api/movie")
                         ?? new List<MovieOption>();

            var vm = new BookingIndexViewModel { Movies = movies };

            // Thông tin member (nếu có)
            ViewBag.MemberId = SessionHelper.GetMemberId(HttpContext.Session);
            ViewBag.FullName = SessionHelper.GetFullName(HttpContext.Session);
            ViewBag.Role = SessionHelper.GetRole(HttpContext.Session);

            return View(vm);
        }

        // GET: /Booking/GetShowtimes?movieId=1  (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetShowtimes(int movieId)
        {
            // Tùy API của bạn – có thể là api/showtime?movieId=1 hoặc api/showtime/by-movie/1
            var list = await _api.GetAsync<List<ShowtimeOption>>($"api/showtime?movieId={movieId}")
                       ?? new List<ShowtimeOption>();

            // Chỉ lấy suất trong tương lai
            list = list.Where(s => s.StartTime > DateTime.Now).ToList();

            return Json(list);
        }

        // GET: /Booking/SeatMap?showtimeId=1
        [HttpGet]
        public async Task<IActionResult> SeatMap(int showtimeId)
        {
            var map = await _api.GetAsync<SeatMapVm>($"api/booking/seatmap/{showtimeId}");
            if (map == null)
                return NotFound();

            ViewBag.MemberId = SessionHelper.GetMemberId(HttpContext.Session);
            return View(map);
        }

        // POST: /Booking/Create  (AJAX hoặc form)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingVm model)
        {
            if (model.SeatIds == null || model.SeatIds.Count == 0)
                return BadRequest(new { success = false, message = "Vui lòng chọn ít nhất một ghế." });

            // Gán MemberId từ Session nếu user là Member
            var memberId = SessionHelper.GetMemberId(HttpContext.Session);
            if (memberId.HasValue)
                model.MemberId = memberId;

            var (success, data, message) = await _api.PostAsync<object>("api/booking", new
            {
                showtimeId = model.ShowtimeId,
                seatIds = model.SeatIds,
                memberId = model.MemberId
            });

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new { success = true, message, data });
        }
    }
}