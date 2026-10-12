using System.Text.Json;
using CinemaManagement.MVC.Helpers;
using CinemaManagement.MVC.Services;
using CinemaManagement.MVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CinemaManagement.MVC.Controllers
{
    [AuthorizeSession(Roles = new[] { "Admin" })]
    public class MovieController : Controller
    {
        private const long MaxPosterBytes = 3 * 1024 * 1024;   // 3 MB
        private static readonly string[] AllowedPosterExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly ApiService _api;

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
        public async Task<IActionResult> Create(MovieViewModel model, IFormFile? posterFile)
        {
            var posterError = ValidatePoster(posterFile);
            if (posterError != null)
                ModelState.AddModelError("posterFile", posterError);

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
                releaseDate = model.ReleaseDate
            });

            if (!success)
            {
                ModelState.AddModelError("", "Thêm phim thất bại: " + message);
                await LoadGenres();
                return View(model);
            }

            // Có chọn ảnh: tải ảnh lên cho phim vừa tạo
            if (posterFile != null && posterFile.Length > 0)
            {
                var newId = TryReadMovieId(message);

                if (newId == null)
                {
                    TempData["Error"] = "Đã thêm phim nhưng không xác định được mã phim để tải ảnh. Vui lòng sửa phim và chọn lại ảnh.";
                    return RedirectToAction(nameof(Index));
                }

                var (ok, uploadMessage) = await _api.PostFileAsync($"api/movie/{newId}/poster", "file", posterFile);
                if (!ok)
                {
                    TempData["Error"] = "Đã thêm phim nhưng tải ảnh thất bại: " + uploadMessage;
                    return RedirectToAction(nameof(Edit), new { id = newId });
                }
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
        public async Task<IActionResult> Edit(int id, MovieViewModel model, IFormFile? posterFile)
        {
            if (id != model.MovieID)
                return BadRequest();

            var posterError = ValidatePoster(posterFile);
            if (posterError != null)
                ModelState.AddModelError("posterFile", posterError);

            if (!ModelState.IsValid)
            {
                await LoadGenres();
                return View(model);
            }

            var (success, message) = await _api.PutAsync($"api/movie/{id}", new
            {
                title = model.Title,
                director = model.Director,
                duration = model.Duration,
                genreID = model.GenreID,
                ageRating = model.AgeRating,
                description = model.Description,
                releaseDate = model.ReleaseDate,
                isActive = model.IsActive
            });

            if (!success)
            {
                ModelState.AddModelError("", "Cập nhật thất bại: " + message);
                await LoadGenres();
                return View(model);
            }

            // Có chọn ảnh mới: thay poster
            if (posterFile != null && posterFile.Length > 0)
            {
                var (ok, uploadMessage) = await _api.PostFileAsync($"api/movie/{id}/poster", "file", posterFile);
                if (!ok)
                {
                    TempData["Error"] = "Đã cập nhật phim nhưng tải ảnh thất bại: " + uploadMessage;
                    return RedirectToAction(nameof(Edit), new { id });
                }
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

        // ---------- Hàm phụ ----------

        private async Task LoadGenres()
        {
            var genres = await _api.GetAsync<List<GenreItem>>("api/genre")
                         ?? new List<GenreItem>();
            ViewBag.Genres = genres;
        }

        // Kiểm tra nhanh ảnh phía MVC (API vẫn kiểm tra lại lần nữa)
        private static string? ValidatePoster(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return null;

            if (file.Length > MaxPosterBytes)
                return "Ảnh poster tối đa 3 MB.";

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedPosterExtensions.Contains(ext))
                return "Chỉ chấp nhận ảnh .jpg, .jpeg, .png hoặc .webp.";

            return null;
        }

        // Đọc movieId từ response của POST api/movie: { "message": "...", "movieId": 12 }
        private static int? TryReadMovieId(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return null;

                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Name.Equals("movieId", StringComparison.OrdinalIgnoreCase)
                        && prop.Value.TryGetInt32(out var id))
                        return id;
                }
            }
            catch (JsonException)
            {
            }

            return null;
        }
    }
}