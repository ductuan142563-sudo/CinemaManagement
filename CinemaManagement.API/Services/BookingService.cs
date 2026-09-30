using CinemaManagement.API.Data;
using CinemaManagement.API.Models;          
using CinemaManagement.BLL.DTOs;
using CinemaManagement.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CinemaManagement.API.Services
{
    public class BookingService
    {
        private readonly CinemaDbContext _context;
        private readonly BookingRepository _bookingRepo;
        private readonly MemberRepository _memberRepo;
        private readonly MembershipService _membershipService;

        public BookingService(CinemaDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _bookingRepo = new BookingRepository(context);
            _memberRepo = new MemberRepository(context);
            _membershipService = new MembershipService(context);
        }

        /// <summary>
        /// Tạo booking đầy đủ: kiểm tra ghế trùng → tính tiền + membership → lưu Booking + Detail → cộng điểm.
        /// </summary>
        public BookingResult CreateBooking(int showtimeId, List<int> seatIds, int? memberId)
        {
            if (seatIds == null || seatIds.Count == 0)
                return BookingResult.Fail("Vui lòng chọn ít nhất một ghế.");

            // Loại bỏ ghế trùng trong danh sách chọn
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

                // 3. Tính baseAmount
                decimal baseAmount = showtime.BasePrice * seatIds.Count;
                decimal discount = 0;
                int pointsEarned = 0;
                Member? member = null;

                // 4. Áp dụng Membership (nếu có)
                if (memberId.HasValue && memberId.Value > 0)
                {
                    // Ưu tiên lấy theo MemberID + Include Tier
                    member = _context.Members
                        .Include(m => m.Tier)
                        .FirstOrDefault(m => m.MemberID == memberId.Value);

                    // Fallback: thử theo UserID nếu repository có method GetByUserId
                    if (member == null)
                    {
                        try
                        {
                            member = _memberRepo.GetByUserId(memberId.Value);
                        }
                        catch
                        {
                            // bỏ qua nếu method không tồn tại
                        }
                    }

                    if (member == null)
                        return BookingResult.Fail("Thành viên không tồn tại.");

                    if (member.Tier == null)
                        return BookingResult.Fail("Hạng thành viên không hợp lệ.");

                    discount = _membershipService.CalculateDiscount(baseAmount, member.Tier);
                    pointsEarned = _membershipService.CalculatePoints(baseAmount - discount, member.Tier);
                }

                // 5. finalAmount
                decimal finalAmount = baseAmount - discount;
                if (finalAmount < 0) finalAmount = 0;

                // 6. Tạo Booking (CHỈ GÁN CÁC PROPERTY MODEL THẬT SỰ CÓ)
                var booking = new Booking
                {
                    BookingCode = _bookingRepo.GenerateBookingCode(),
                    ShowtimeID = showtimeId,
                    MemberID = member?.MemberID,
                    TotalAmount = finalAmount,
                    Status = "Paid",
                    CreatedAt = DateTime.Now
                };

                // Nếu model của bạn CÓ các field sau thì bỏ comment:
                // booking.DiscountAmount = discount;
                // booking.PointsEarned = pointsEarned;

                _context.Bookings.Add(booking);
                _context.SaveChanges(); // lấy BookingID

                // 7. Tạo BookingDetails
                foreach (var seatId in seatIds)
                {
                    var detail = new BookingDetail
                    {
                        BookingID = booking.BookingID,
                        SeatID = seatId,
                        Price = showtime.BasePrice
                    };
                    _context.BookingDetails.Add(detail);
                }

                _context.SaveChanges();

                // 8. Cộng điểm + kiểm tra nâng hạng
                if (member != null && pointsEarned > 0)
                {
                    _membershipService.AddPointsAndCheckUpgrade(member.MemberID, pointsEarned);
                }

                // Reload đầy đủ
                booking = _context.Bookings
                    .Include(b => b.BookingDetails)
                    .FirstOrDefault(b => b.BookingID == booking.BookingID);

                string msg = $"Đặt vé thành công! Mã: {booking!.BookingCode}. Thanh toán: {finalAmount:N0}đ";
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
        /// Hủy booking theo mã (optional)
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

                // Nếu model có UpdatedAt thì bỏ comment:
                // booking.UpdatedAt = DateTime.Now;

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