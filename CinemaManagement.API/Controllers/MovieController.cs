using CinemaManagement.API.Data;
using CinemaManagement.API.DTOs;
using CinemaManagement.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MovieController : ControllerBase
    {
        private readonly CinemaDbContext _context;

        public MovieController(CinemaDbContext context)
        {
            _context = context;
        }

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
                IsActive = movie.IsActive
            });
        }

        /// <summary>POST /api/movie</summary>
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

        /// <summary>PUT /api/movie/5</summary>
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
            movie.IsActive = request.IsActive;

            _context.SaveChanges();

            return Ok(new { message = "Cập nhật phim thành công." });
        }

        /// <summary>DELETE /api/movie/5</summary>
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
    }
}