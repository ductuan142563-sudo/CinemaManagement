using CinemaManagement.API.Data;
using CinemaManagement.API.Models;
using CinemaManagement.DAL.Repositories;   
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.API.Services
{
    public class BookingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Booking? Booking { get; set; }

        public static BookingResult Ok(Booking booking, string message)
            => new() { Success = true, Booking = booking, Message = message };

        public static BookingResult Fail(string message)
            => new() { Success = false, Message = message };
    }

    public class BookingService
    {
        private readonly CinemaDbContext _context;
        private readonly BookingRepository _bookingRepo;
        private readonly MembershipService _membershipService;

        public BookingService(CinemaDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _bookingRepo = new BookingRepository(context);
            _membershipService = new MembershipService(context);
        }

        /// <summary>
        /// Tạo booking đầy đủ
        /// </summary>
        public BookingResult CreateBooking(int showtimeId, List<int> seatIds, int? memberId)
        {
            if (seatIds == null || seatIds.Count == 0)
                return BookingResult.Fail("Vui lòng chọn ít nhất một ghế.");

            // Loại bỏ ghế trùng trong danh sách
            seatIds = seatIds.Distinct().ToList();

            try
            {
                // 1. Kiểm tra ghế đã bị đặt chưa
                var bookedSeatIds = _bookingRepo.GetBookedSeatIds(showtimeId);
                var conflictSeats = seatIds.Intersect(bookedSeatIds).ToList();

                if (conflictSeats.Count > 0)
                {
                    return BookingResult.Fail(
                        $"Ghế đã được đặt: {string.Join(", ", conflictSeats)}. Vui lòng chọn ghế khác.");
                }

                // 2. Lấy Showtime
                var showtime = _context.Showtimes
                    .FirstOrDefault(s => s.ShowtimeID == showtimeId);

                if (showtime == null)
                    return BookingResult.Fail("Suất chiếu không tồn tại.");

                if (showtime.StartTime <= DateTime.Now)
                    return BookingResult.Fail("Không thể đặt vé cho suất chiếu đã bắt đầu hoặc đã qua.");

                // 3. Tính tiền
                decimal baseAmount = showtime.BasePrice * seatIds.Count;
                decimal discount = 0;
                int pointsEarned = 0;
                Member? member = null;

                if (memberId.HasValue && memberId.Value > 0)
                {
                    member = _context.Members
                        .Include(m => m.Tier)
                        .FirstOrDefault(m => m.MemberID == memberId.Value);

                    if (member == null)
                        return BookingResult.Fail("Thành viên không tồn tại.");

                    if (member.Tier == null)
                        return BookingResult.Fail("Hạng thành viên không hợp lệ.");

                    discount = _membershipService.CalculateDiscount(baseAmount, member.Tier);
                    pointsEarned = _membershipService.CalculatePoints(baseAmount - discount, member.Tier);
                }

                decimal finalAmount = Math.Max(0, baseAmount - discount);

                // 4. Tạo Booking (khớp model thật của bạn)
                var booking = new Booking
                {
                    BookingCode = _bookingRepo.GenerateBookingCode(),
                    ShowtimeID = showtimeId,
                    MemberID = member?.MemberID,
                    TotalAmount = baseAmount,        // tiền gốc
                    DiscountAmount = discount,       // tiền giảm
                    FinalAmount = finalAmount,       // tiền sau giảm
                    PointsEarned = pointsEarned,
                    Status = "Paid",
                    CreatedAt = DateTime.Now
                };

                _context.Bookings.Add(booking);
                _context.SaveChanges(); // lấy BookingID

                // 5. Tạo BookingDetails
                foreach (var seatId in seatIds)
                {
                    _context.BookingDetails.Add(new BookingDetail
                    {
                        BookingID = booking.BookingID,
                        SeatID = seatId,
                        Price = showtime.BasePrice
                    });
                }
                _context.SaveChanges();

                // 6. Cộng điểm + kiểm tra nâng hạng
                if (member != null && pointsEarned > 0)
                {
                    _membershipService.AddPointsAndCheckUpgrade(member.MemberID, pointsEarned);
                }

                // Reload đầy đủ
                booking = _context.Bookings
                    .Include(b => b.BookingDetails)
                    .FirstOrDefault(b => b.BookingID == booking.BookingID)!;

                string msg = $"Đặt vé thành công! Mã: {booking.BookingCode}. Thanh toán: {finalAmount:N0}đ";
                if (pointsEarned > 0)
                    msg += $". Điểm nhận: +{pointsEarned}";

                return BookingResult.Ok(booking, msg);
            }
            catch (Exception ex)
            {
                return BookingResult.Fail($"Lỗi khi đặt vé: {ex.Message}");
            }
        }

        /// <summary>
        /// Hủy booking theo mã
        /// </summary>
        public BookingResult CancelBooking(string bookingCode)
        {
            if (string.IsNullOrWhiteSpace(bookingCode))
                return BookingResult.Fail("Mã booking không hợp lệ.");

            try
            {
                var booking = _context.Bookings
                    .Include(b => b.BookingDetails)
                    .FirstOrDefault(b => b.BookingCode == bookingCode);

                if (booking == null)
                    return BookingResult.Fail("Không tìm thấy booking.");

                if (booking.Status == "Cancelled")
                    return BookingResult.Fail("Booking đã bị hủy trước đó.");

                var showtime = _context.Showtimes.Find(booking.ShowtimeID);
                if (showtime != null && showtime.StartTime <= DateTime.Now)
                    return BookingResult.Fail("Không thể hủy vé của suất chiếu đã bắt đầu.");

                booking.Status = "Cancelled";
                _context.SaveChanges();

                return BookingResult.Ok(booking, "Hủy booking thành công.");
            }
            catch (Exception ex)
            {
                return BookingResult.Fail($"Lỗi khi hủy booking: {ex.Message}");
            }
        }
    }
}