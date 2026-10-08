using CinemaManagement.MVC.Helpers;
using CinemaManagement.MVC.Services;
using CinemaManagement.MVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CinemaManagement.MVC.Controllers
{
    [AuthorizeSession(Roles = new[] { "Admin", "Staff" })]
    public class ShowtimeController : Controller
    {
        private readonly ApiService _api;

        public ShowtimeController(ApiService api)
        {
            _api = api;
        }

        // GET: /Showtime
        public async Task<IActionResult> Index()
        {
            var list = await _api.GetAsync<List<ShowtimeViewModel>>("api/showtime")
                       ?? new List<ShowtimeViewModel>();
            return View(list);
        }

        // GET: /Showtime/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new ShowtimeViewModel
            {
                StartTime = DateTime.Now.AddHours(1),
                BasePrice = 50000
            });
        }

        // POST: /Showtime/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShowtimeViewModel model)
        {
            if (model.StartTime <= DateTime.Now)
                ModelState.AddModelError("StartTime", "Suất chiếu phải trong tương lai.");

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(model);
            }

            var (success, message) = await _api.PostAsync("api/showtime", new
            {
                movieID = model.MovieID,
                hallID = model.HallID,
                startTime = model.StartTime,
                basePrice = model.BasePrice,
                isActive = true
            });

            if (!success)
            {
                ModelState.AddModelError("", "Tạo suất chiếu thất bại: " + message);
                await LoadDropdowns();
                return View(model);
            }

            TempData["Success"] = "Tạo suất chiếu thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Showtime/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, message) = await _api.DeleteAsync($"api/showtime/{id}");
            TempData[success ? "Success" : "Error"] = success
                ? "Xóa suất chiếu thành công!"
                : "Xóa thất bại: " + message;
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns()
        {
            ViewBag.Movies = await _api.GetAsync<List<MovieViewModel>>("api/movie")
                             ?? new List<MovieViewModel>();
            // Nếu có API halls:
            ViewBag.Halls = await _api.GetAsync<List<SelectItem>>("api/hall")
                            ?? new List<SelectItem>();
        }
    }
}