using CinemaManagement.API.Data;
using CinemaManagement.API.DTOs;
using CinemaManagement.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly BookingService _bookingService;
        private readonly CinemaDbContext _context;

        public BookingController(BookingService bookingService, CinemaDbContext context)
        {
            _bookingService = bookingService;
            _context = context;
        }

        /// <summary>
        /// Lấy sơ đồ ghế của suất chiếu
        /// GET: api/booking/seatmap/1
        /// </summary>
        [HttpGet("seatmap/{showtimeId}")]
        public async Task<ActionResult<ShowtimeSeatMapResponse>> GetSeatMap(int showtimeId)
        {
            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                    .ThenInclude(h => h.Seats)
                .FirstOrDefaultAsync(s => s.ShowtimeID == showtimeId);

            if (showtime == null)
                return NotFound(new { message = "Suất chiếu không tồn tại." });

            // Lấy ghế đã đặt
            var bookedSeatIds = await _context.BookingDetails
                .Include(bd => bd.Booking)
                .Where(bd => bd.Booking.ShowtimeID == showtimeId && bd.Booking.Status == "Paid")
                .Select(bd => bd.SeatID)
                .ToListAsync();

            var seats = showtime.Hall.Seats
                .OrderBy(s => s.RowLabel)
                .ThenBy(s => s.SeatNumber)
                .Select(s => new SeatStatusDto
                {
                    SeatId = s.SeatID,
                    RowLabel = s.RowLabel,
                    SeatNumber = s.SeatNumber,
                    SeatType = s.SeatType,
                    IsBooked = bookedSeatIds.Contains(s.SeatID)
                })
                .ToList();

            var response = new ShowtimeSeatMapResponse
            {
                ShowtimeId = showtime.ShowtimeID,
                MovieTitle = showtime.Movie?.Title ?? "",
                HallName = showtime.Hall?.HallName ?? "",
                StartTime = showtime.StartTime,
                BasePrice = showtime.BasePrice,
                Seats = seats
            };

            return Ok(response);
        }

        /// <summary>
        /// Đặt vé
        /// POST: api/booking
        /// </summary>
        [HttpPost]
        public ActionResult<BookingResponse> CreateBooking([FromBody] CreateBookingRequest request)
        {
            if (request.SeatIds == null || request.SeatIds.Count == 0)
            {
                return BadRequest(new BookingResponse
                {
                    Success = false,
                    Message = "Vui lòng chọn ít nhất một ghế."
                });
            }

            var result = _bookingService.CreateBooking(
                request.ShowtimeId,
                request.SeatIds,
                request.MemberId
            );

            var response = new BookingResponse
            {
                Success = result.Success,
                Message = result.Message,
                BookingCode = result.Booking?.BookingCode,
                FinalAmount = result.Booking?.FinalAmount ?? 0,
                PointsEarned = result.Booking?.PointsEarned ?? 0,
                TotalSeats = request.SeatIds.Count
            };

            if (!result.Success)
                return BadRequest(response);

            return Ok(response);
        }

        /// <summary>
        /// Hủy vé theo mã booking
        /// PUT: api/booking/cancel/{bookingCode}
        /// </summary>
        [HttpPut("cancel/{bookingCode}")]
        public ActionResult<BookingResponse> CancelBooking(string bookingCode)
        {
            var result = _bookingService.CancelBooking(bookingCode);

            var response = new BookingResponse
            {
                Success = result.Success,
                Message = result.Message,
                BookingCode = result.Booking?.BookingCode
            };

            if (!result.Success)
                return BadRequest(response);

            return Ok(response);
        }
    }
}