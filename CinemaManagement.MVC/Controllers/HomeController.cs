using CinemaManagement.MVC.Helpers;
using CinemaManagement.MVC.Services;
using CinemaManagement.MVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CinemaManagement.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiService _api;

        public HomeController(ApiService api)
        {
            _api = api;
        }

        // GET: /
        public async Task<IActionResult> Index()
        {
            var vm = new HomePageViewModel();

            // Khách chưa đăng nhập: chỉ hiện logo, không gọi API
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return View(vm);

            vm.IsLoggedIn = true;

            try
            {
                var movies = await _api.GetAsync<List<HomeMovieDto>>("api/movie");
                var showtimes = await _api.GetAsync<List<HomeShowtimeDto>>("api/showtime");

                // Token hết hạn: ApiService đã xóa Session -> đưa về trang đăng nhập
                if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                    return RedirectToAction("Login", "Account");

                if (movies == null || showtimes == null)
                {
                    vm.LoadFailed = true;
                    return View(vm);
                }

                var now = DateTime.Now;

                var upcoming = showtimes
                    .Where(s => s.IsActive && s.StartTime > now)
                    .OrderBy(s => s.StartTime)
                    .ToList();

                vm.Cards = movies
                    .Where(m => m.IsActive)
                    .Select(m => new HomeMovieCard
                    {
                        MovieId = m.MovieID,
                        Title = m.Title,
                        Duration = m.Duration,
                        GenreName = m.GenreName,
                        AgeRating = m.AgeRating,
                        Showtimes = upcoming.Where(s => s.MovieID == m.MovieID).ToList()
                    })
                    .Where(c => c.Showtimes.Count > 0)
                    .OrderBy(c => c.Showtimes[0].StartTime)
                    .ToList();

                var first = vm.Cards.FirstOrDefault();
                if (first != null)
                {
                    vm.NextMovie = first;
                    vm.NextShowtime = first.Showtimes[0];
                }
            }
            catch (Exception)
            {
                vm.LoadFailed = true;
            }

            return View(vm);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}