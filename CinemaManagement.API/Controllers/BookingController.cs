using CinemaManagement.API.Data;
using CinemaManagement.API.DTOs;
using CinemaManagement.API.Models;
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
            // Không Include Hall.Seats nữa: ghế sẽ được truy vấn trực tiếp theo HallID bên dưới
            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .FirstOrDefaultAsync(s => s.ShowtimeID == showtimeId);

            if (showtime == null)
                return NotFound(new { message = "Suất chiếu không tồn tại." });

            // Lấy ghế đã đặt
            var bookedSeatIds = (await _context.BookingDetails
                .Include(bd => bd.Booking)
                .Where(bd => bd.Booking.ShowtimeID == showtimeId && bd.Booking.Status == "Paid")
                .Select(bd => bd.SeatID)
                .ToListAsync())
                .ToHashSet();

            // Lấy toàn bộ ghế của phòng chiếu trực tiếp từ bảng Seats
            var hallId = showtime.Hall?.HallID ?? 0;

            var hallSeats = await _context.Seats
                .Where(s => s.HallID == hallId)
                .OrderBy(s => s.RowLabel)
                .ThenBy(s => s.SeatNumber)
                .ToListAsync();

            var seats = hallSeats
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
        /// Tạo ghế cho tất cả Hall chưa có ghế
        /// GET: api/booking/api/seed/seats
        /// </summary>
        [HttpGet("api/seed/seats")]
        public async Task<IActionResult> SeedSeats()
        {
            var halls = await _context.Halls.ToListAsync();

            int totalAdded = 0;
            string rowLabels = "ABCDEFGHIJKLMNOP";

            foreach (var hall in halls)
            {
                // Kiểm tra trực tiếp bảng Seats (không dùng hall.Seats) để tránh tạo trùng
                bool hasSeats = await _context.Seats.AnyAsync(s => s.HallID == hall.HallID);
                if (hasSeats)
                    continue; // đã có ghế rồi

                int rows = hall.RowsCount > 0 ? hall.RowsCount : 8;
                int cols = hall.SeatsPerRow > 0 ? hall.SeatsPerRow : 10;

                var seats = new List<Seat>();
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 1; c <= cols; c++)
                    {
                        seats.Add(new Seat
                        {
                            HallID = hall.HallID,
                            RowLabel = rowLabels[r].ToString(),
                            SeatNumber = c,
                            SeatType = "Standard"
                        });
                    }
                }

                _context.Seats.AddRange(seats);
                totalAdded += seats.Count;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Đã tạo {totalAdded} ghế.",
                halls = halls.Count
            });
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