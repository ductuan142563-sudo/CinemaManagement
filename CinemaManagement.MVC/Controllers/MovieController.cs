using CinemaManagement.MVC.Helpers;
using CinemaManagement.MVC.Services;
using CinemaManagement.MVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CinemaManagement.MVC.Controllers
{
    [AuthorizeSession(Roles = new[] { "Admin" })]
    public class MovieController : Controller
    {
        private readonly ApiService _api;
        private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        public MovieController(ApiService api)
        {
            _api = api;
        }

        // GET: /Movie
        public async Task<IActionResult> Index()
        {
            var movies = await _api.GetAsync<List<MovieViewModel>>("api/movie")
                         ?? new List<MovieViewModel>();
            return View(movies);
        }

        // GET: /Movie/Create
        public async Task<IActionResult> Create()
        {
            await LoadGenres();
            return View(new MovieViewModel());
        }

        // POST: /Movie/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadGenres();
                return View(model);
            }

            var (success, message) = await _api.PostAsync("api/movie", new
            {
                title = model.Title,
                director = model.Director,
                duration = model.Duration,
                genreID = model.GenreID,
                ageRating = model.AgeRating,
                description = model.Description,
                posterUrl = model.PosterUrl,
                releaseDate = model.ReleaseDate,
                isActive = model.IsActive
            });

            if (!success)
            {
                ModelState.AddModelError("", "Thêm phim thất bại: " + message);
                await LoadGenres();
                return View(model);
            }

            TempData["Success"] = "Thêm phim thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Movie/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var movie = await _api.GetAsync<MovieViewModel>($"api/movie/{id}");
            if (movie == null)
                return NotFound();

            await LoadGenres();
            return View(movie);
        }

        // POST: /Movie/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MovieViewModel model)
        {
            if (id != model.MovieID)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadGenres();
                return View(model);
            }

            var (success, message) = await _api.PutAsync($"api/movie/{id}", new
            {
                movieID = model.MovieID,
                title = model.Title,
                director = model.Director,
                duration = model.Duration,
                genreID = model.GenreID,
                ageRating = model.AgeRating,
                description = model.Description,
                posterUrl = model.PosterUrl,
                releaseDate = model.ReleaseDate,
                isActive = model.IsActive
            });

            if (!success)
            {
                ModelState.AddModelError("", "Cập nhật thất bại: " + message);
                await LoadGenres();
                return View(model);
            }

            TempData["Success"] = "Cập nhật phim thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Movie/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, message) = await _api.DeleteAsync($"api/movie/{id}");

            TempData[success ? "Success" : "Error"] = success
                ? "Xóa phim thành công!"
                : "Xóa thất bại: " + message;

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadGenres()
        {
            var genres = await _api.GetAsync<List<GenreItem>>("api/genre")
                         ?? new List<GenreItem>();
            ViewBag.Genres = genres;
        }
    }
}