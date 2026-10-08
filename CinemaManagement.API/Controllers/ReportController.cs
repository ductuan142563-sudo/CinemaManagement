using CinemaManagement.API.Data;
using CinemaManagement.API.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly CinemaDbContext _context;

        public ReportController(CinemaDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Doanh thu theo khoảng ngày
        /// GET: api/report/revenue?fromDate=2025-01-01&toDate=2025-12-31
        /// </summary>
        [HttpGet("revenue")]
        public async Task<ActionResult<List<RevenueByDateDto>>> GetRevenueByDate(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            if (fromDate > toDate)
                return BadRequest(new { message = "fromDate không được lớn hơn toDate." });

            var result = await _context.Bookings
                .Where(b => b.Status == "Paid"
                         && b.CreatedAt.Date >= fromDate.Date
                         && b.CreatedAt.Date <= toDate.Date)
                .GroupBy(b => b.CreatedAt.Date)
                .Select(g => new RevenueByDateDto
                {
                    Date = g.Key,
                    TotalBookings = g.Count(),
                    TotalTickets = g.Sum(b => b.BookingDetails.Count),
                    TotalRevenue = g.Sum(b => b.FinalAmount)
                })
                .OrderBy(r => r.Date)
                .ToListAsync();

            return Ok(result);
        }

        /// <summary>
        /// Top phim theo doanh thu
        /// GET: api/report/top-movies?fromDate=2025-01-01&toDate=2025-12-31&top=5
        /// </summary>
        [HttpGet("top-movies")]
        public async Task<ActionResult<List<TopMovieDto>>> GetTopMovies(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] int top = 5)
        {
            if (fromDate > toDate)
                return BadRequest(new { message = "fromDate không được lớn hơn toDate." });

            if (top <= 0) top = 5;

            var result = await _context.Bookings
                .Where(b => b.Status == "Paid"
                         && b.CreatedAt.Date >= fromDate.Date
                         && b.CreatedAt.Date <= toDate.Date)
                .SelectMany(b => b.BookingDetails.Select(bd => new
                {
                    MovieId = b.Showtime.MovieID,
                    MovieTitle = b.Showtime.Movie.Title,
                    Price = bd.Price
                }))
                .GroupBy(x => new { x.MovieId, x.MovieTitle })
                .Select(g => new TopMovieDto
                {
                    MovieId = g.Key.MovieId,
                    MovieTitle = g.Key.MovieTitle,
                    TotalTickets = g.Count(),
                    TotalRevenue = g.Sum(x => x.Price)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(top)
                .ToListAsync();

            return Ok(result);
        }

        /// <summary>
        /// Số lượng vé bán theo ngày
        /// GET: api/report/tickets?fromDate=2025-01-01&toDate=2025-12-31
        /// </summary>
        [HttpGet("tickets")]
        public async Task<ActionResult<List<RevenueByDateDto>>> GetTicketsByDate(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            if (fromDate > toDate)
                return BadRequest(new { message = "fromDate không được lớn hơn toDate." });

            var result = await _context.Bookings
                .Where(b => b.Status == "Paid"
                         && b.CreatedAt.Date >= fromDate.Date
                         && b.CreatedAt.Date <= toDate.Date)
                .GroupBy(b => b.CreatedAt.Date)
                .Select(g => new RevenueByDateDto
                {
                    Date = g.Key,
                    TotalBookings = g.Count(),
                    TotalTickets = g.Sum(b => b.BookingDetails.Count),
                    TotalRevenue = g.Sum(b => b.FinalAmount)
                })
                .OrderBy(r => r.Date)
                .ToListAsync();

            return Ok(result);
        }

        /// <summary>
        /// Tổng quan nhanh (dashboard)
        /// GET: api/report/summary
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var today = DateTime.Today;

            var totalRevenue = await _context.Bookings
                .Where(b => b.Status == "Paid")
                .SumAsync(b => (decimal?)b.FinalAmount) ?? 0;

            var todayRevenue = await _context.Bookings
                .Where(b => b.Status == "Paid" && b.CreatedAt.Date == today)
                .SumAsync(b => (decimal?)b.FinalAmount) ?? 0;

            var totalTickets = await _context.BookingDetails
                .CountAsync(bd => bd.Booking.Status == "Paid");

            var totalMembers = await _context.Members.CountAsync();

            return Ok(new
            {
                TotalRevenue = totalRevenue,
                TodayRevenue = todayRevenue,
                TotalTickets = totalTickets,
                TotalMembers = totalMembers
            });
        }
    }
}