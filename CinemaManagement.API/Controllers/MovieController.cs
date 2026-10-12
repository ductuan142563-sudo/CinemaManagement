using CinemaManagement.API.Data;
using CinemaManagement.API.DTOs;
using CinemaManagement.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]   // phải đăng nhập mới gọi được; riêng thêm/sửa/xóa/tải ảnh chỉ Admin
    public class MovieController : ControllerBase
    {
        private const long MaxPosterBytes = 3 * 1024 * 1024;   // 3 MB
        private static readonly string[] AllowedPosterExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly CinemaDbContext _context;
        private readonly IWebHostEnvironment _env;

        public MovieController(CinemaDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Thư mục lưu poster (phải trùng với thư mục khai báo trong Program.cs)
        private string PosterFolder => Path.Combine(_env.ContentRootPath, "wwwroot", "posters");

        /// <summary>GET /api/movie</summary>
        [HttpGet]
        public IActionResult GetAll([FromQuery] string? search = null)
        {
            var query = _context.Movies
                .Include(m => m.Genre)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(m =>
                    m.Title.ToLower().Contains(search) ||
                    (m.Director != null && m.Director.ToLower().Contains(search)));
            }

            var list = query
                .OrderByDescending(m => m.MovieID)
                .Select(m => new MovieDto
                {
                    MovieID = m.MovieID,
                    Title = m.Title,
                    Director = m.Director,
                    Duration = m.Duration,
                    GenreID = m.GenreID,
                    GenreName = m.Genre != null ? m.Genre.GenreName : null,
                    AgeRating = m.AgeRating,
                    Description = m.Description,
                    ReleaseDate = m.ReleaseDate,
                    PosterUrl = m.PosterUrl,
                    IsActive = m.IsActive
                })
                .ToList();

            return Ok(list);
        }

        /// <summary>GET /api/movie/5</summary>
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var movie = _context.Movies
                .Include(m => m.Genre)
                .FirstOrDefault(m => m.MovieID == id);

            if (movie == null)
                return NotFound(new { message = "Không tìm thấy phim." });

            return Ok(new MovieDto
            {
                MovieID = movie.MovieID,
                Title = movie.Title,
                Director = movie.Director,
                Duration = movie.Duration,
                GenreID = movie.GenreID,
                GenreName = movie.Genre?.GenreName,
                AgeRating = movie.AgeRating,
                Description = movie.Description,
                ReleaseDate = movie.ReleaseDate,
                PosterUrl = movie.PosterUrl,
                IsActive = movie.IsActive
            });
        }

        /// <summary>POST /api/movie (chỉ Admin)</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Create([FromBody] MovieCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new { message = "Title không được để trống." });

            if (request.Duration <= 0)
                return BadRequest(new { message = "Duration phải > 0." });

            var movie = new Movie
            {
                Title = request.Title.Trim(),
                Director = request.Director?.Trim(),
                Duration = request.Duration,
                GenreID = request.GenreID,
                AgeRating = request.AgeRating?.Trim(),
                Description = request.Description?.Trim(),
                ReleaseDate = request.ReleaseDate,
                IsActive = true
            };

            _context.Movies.Add(movie);
            _context.SaveChanges();

            return CreatedAtAction(nameof(GetById), new { id = movie.MovieID }, new
            {
                message = "Thêm phim thành công.",
                movieId = movie.MovieID
            });
        }

        /// <summary>PUT /api/movie/5 (chỉ Admin)</summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] MovieUpdateRequest request)
        {
            var movie = _context.Movies.Find(id);
            if (movie == null)
                return NotFound(new { message = "Không tìm thấy phim." });

            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new { message = "Title không được để trống." });

            if (request.Duration <= 0)
                return BadRequest(new { message = "Duration phải > 0." });

            movie.Title = request.Title.Trim();
            movie.Director = request.Director?.Trim();
            movie.Duration = request.Duration;
            movie.GenreID = request.GenreID;
            movie.AgeRating = request.AgeRating?.Trim();
            movie.Description = request.Description?.Trim();
            movie.ReleaseDate = request.ReleaseDate;
            movie.IsActive = request.IsActive;

            _context.SaveChanges();

            return Ok(new { message = "Cập nhật phim thành công." });
        }

        /// <summary>
        /// Tải ảnh poster cho phim (chỉ Admin)
        /// POST /api/movie/5/poster  (multipart/form-data, field tên "file")
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/poster")]
        [RequestSizeLimit(4 * 1024 * 1024)]
        public async Task<IActionResult> UploadPoster(int id, IFormFile? file)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
                return NotFound(new { message = "Không tìm thấy phim." });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Vui lòng chọn ảnh poster." });

            if (file.Length > MaxPosterBytes)
                return BadRequest(new { message = "Ảnh poster tối đa 3 MB." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedPosterExtensions.Contains(ext))
                return BadRequest(new { message = "Chỉ chấp nhận ảnh .jpg, .jpeg, .png hoặc .webp." });

            if (!await HasValidImageSignatureAsync(file, ext))
                return BadRequest(new { message = "Tệp tải lên không phải ảnh hợp lệ." });

            Directory.CreateDirectory(PosterFolder);

            // Tên file ngẫu nhiên: không dùng tên file người dùng gửi lên
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(PosterFolder, fileName);

            await using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            DeleteOldPoster(movie.PosterUrl);

            movie.PosterUrl = $"/posters/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Tải ảnh poster thành công.",
                posterUrl = movie.PosterUrl
            });
        }

        /// <summary>DELETE /api/movie/5 (chỉ Admin)</summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var movie = _context.Movies.Find(id);
            if (movie == null)
                return NotFound(new { message = "Không tìm thấy phim." });

            // Soft delete (khuyên dùng)
            movie.IsActive = false;
            _context.SaveChanges();

            // Nếu muốn xóa cứng:
            // _context.Movies.Remove(movie);
            // _context.SaveChanges();

            return Ok(new { message = "Xóa phim thành công." });
        }

        // ---------- Hàm phụ ----------

        // Kiểm tra vài byte đầu của tệp để chắc chắn đúng là JPG/PNG/WebP (không tin đuôi tên file)
        private static async Task<bool> HasValidImageSignatureAsync(IFormFile file, string ext)
        {
            var header = new byte[12];
            await using var stream = file.OpenReadStream();
            var read = await stream.ReadAsync(header, 0, header.Length);
            if (read < header.Length)
                return false;

            return ext switch
            {
                ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
                ".webp" => header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                        && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
                _ => false
            };
        }

        // Xóa file poster cũ khi thay ảnh mới (chỉ xóa file nằm trong thư mục posters)
        private void DeleteOldPoster(string? posterUrl)
        {
            if (string.IsNullOrWhiteSpace(posterUrl) || !posterUrl.StartsWith("/posters/"))
                return;

            var oldPath = Path.Combine(PosterFolder, Path.GetFileName(posterUrl));
            if (System.IO.File.Exists(oldPath))
                System.IO.File.Delete(oldPath);
        }
    }
}