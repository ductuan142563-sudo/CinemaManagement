using CinemaManagement.API.Data;
using CinemaManagement.API.DTOs;
using CinemaManagement.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShowtimeController : ControllerBase
    {
        private readonly CinemaDbContext _context;

        public ShowtimeController(CinemaDbContext context)
        {
            _context = context;
        }

        /// <summary>GET /api/showtime</summary>
        [HttpGet]
        public IActionResult GetAll([FromQuery] int? movieId = null, [FromQuery] bool onlyUpcoming = true)
        {
            var query = _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .AsQueryable();

            if (movieId.HasValue)
                query = query.Where(s => s.MovieID == movieId.Value);

            if (onlyUpcoming)
                query = query.Where(s => s.StartTime > DateTime.Now);

            var list = query
                .OrderBy(s => s.StartTime)
                .Select(s => new ShowtimeDto
                {
                    ShowtimeID = s.ShowtimeID,
                    MovieID = s.MovieID,
                    MovieTitle = s.Movie != null ? s.Movie.Title : null,
                    HallID = s.HallID,
                    HallName = s.Hall != null ? s.Hall.HallName : null,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    BasePrice = s.BasePrice,
                    IsActive = s.IsActive
                })
                .ToList();

            return Ok(list);
        }

        /// <summary>GET /api/showtime/{id}</summary>
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var s = _context.Showtimes
                .Include(x => x.Movie)
                .Include(x => x.Hall)
                .FirstOrDefault(x => x.ShowtimeID == id);

            if (s == null)
                return NotFound(new { message = "Không tìm thấy suất chiếu." });

            return Ok(new ShowtimeDto
            {
                ShowtimeID = s.ShowtimeID,
                MovieID = s.MovieID,
                MovieTitle = s.Movie?.Title,
                HallID = s.HallID,
                HallName = s.Hall?.HallName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                BasePrice = s.BasePrice,
                IsActive = s.IsActive
            });
        }

        /// <summary>POST /api/showtime</summary>
        [HttpPost]
        public IActionResult Create([FromBody] ShowtimeCreateRequest request)
        {
            if (request.StartTime <= DateTime.Now)
                return BadRequest(new { message = "Chỉ được tạo suất chiếu trong tương lai." });

            if (request.BasePrice <= 0)
                return BadRequest(new { message = "BasePrice phải > 0." });

            var movie = _context.Movies.Find(request.MovieID);
            if (movie == null)
                return BadRequest(new { message = "Phim không tồn tại." });

            var hall = _context.Halls.Find(request.HallID);
            if (hall == null)
                return BadRequest(new { message = "Phòng chiếu không tồn tại." });

            // Tự tính EndTime = StartTime + Duration
            var endTime = request.StartTime.AddMinutes(movie.Duration);

            var showtime = new Showtime
            {
                MovieID = request.MovieID,
                HallID = request.HallID,
                StartTime = request.StartTime,
                EndTime = endTime,
                BasePrice = request.BasePrice,
                IsActive = true
            };

            _context.Showtimes.Add(showtime);
            _context.SaveChanges();

            return CreatedAtAction(nameof(GetById), new { id = showtime.ShowtimeID }, new
            {
                message = "Tạo suất chiếu thành công.",
                showtimeId = showtime.ShowtimeID,
                endTime
            });
        }

        /// <summary>PUT /api/showtime/{id}</summary>
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] ShowtimeUpdateRequest request)
        {
            var showtime = _context.Showtimes.Find(id);
            if (showtime == null)
                return NotFound(new { message = "Không tìm thấy suất chiếu." });

            if (request.StartTime <= DateTime.Now)
                return BadRequest(new { message = "Chỉ được đặt suất chiếu trong tương lai." });

            if (request.BasePrice <= 0)
                return BadRequest(new { message = "BasePrice phải > 0." });

            var movie = _context.Movies.Find(request.MovieID);
            if (movie == null)
                return BadRequest(new { message = "Phim không tồn tại." });

            var hall = _context.Halls.Find(request.HallID);
            if (hall == null)
                return BadRequest(new { message = "Phòng chiếu không tồn tại." });

            showtime.MovieID = request.MovieID;
            showtime.HallID = request.HallID;
            showtime.StartTime = request.StartTime;
            showtime.EndTime = request.StartTime.AddMinutes(movie.Duration);
            showtime.BasePrice = request.BasePrice;
            showtime.IsActive = request.IsActive;

            _context.SaveChanges();

            return Ok(new { message = "Cập nhật suất chiếu thành công." });
        }

        /// <summary>DELETE /api/showtime/{id}</summary>
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var showtime = _context.Showtimes.Find(id);
            if (showtime == null)
                return NotFound(new { message = "Không tìm thấy suất chiếu." });

            // Soft delete
            showtime.IsActive = false;
            _context.SaveChanges();

            return Ok(new { message = "Xóa suất chiếu thành công." });
        }
    }
}