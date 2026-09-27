using CinemaManagement.API.Data;
using CinemaManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.DAL.Repositories
{
    public class BookingRepository : GenericRepository<Booking>
    {
        public BookingRepository(CinemaDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Trả về danh sách SeatID đã được đặt (Status = Paid) cho 1 Showtime
        /// </summary>
        public List<int> GetBookedSeatIds(int showtimeId)
        {
            return _context.BookingDetails
                .Where(bd => bd.Booking.ShowtimeID == showtimeId
                          && bd.Booking.Status == "Paid")
                .Select(bd => bd.SeatID)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Sinh mã booking: BK + yyyyMMddHHmmss
        /// Ví dụ: BK20260927143022
        /// </summary>
        public string GenerateBookingCode()
        {
            string code;
            do
            {
                code = "BK" + DateTime.Now.ToString("yyyyMMddHHmmss");

                // Nếu trùng (rất hiếm) thì thêm millisecond
                if (_dbSet.Any(b => b.BookingCode == code))
                {
                    code = "BK" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                }
            } while (_dbSet.Any(b => b.BookingCode == code));

            return code;
        }

        /// <summary>
        /// Lấy Booking theo BookingCode (Include Details + Seat + Showtime)
        /// </summary>
        public Booking? GetByCode(string bookingCode)
        {
            if (string.IsNullOrWhiteSpace(bookingCode))
                return null;

            return _dbSet
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Seat)
                .Include(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Member)
                .FirstOrDefault(b => b.BookingCode == bookingCode);
        }

        /// <summary>
        /// Lấy danh sách Booking theo Showtime
        /// </summary>
        public IEnumerable<Booking> GetByShowtimeId(int showtimeId)
        {
            return _dbSet
                .Include(b => b.BookingDetails)
                .Include(b => b.Member)
                .Where(b => b.ShowtimeID == showtimeId && b.Status == "Paid")
                .AsNoTracking()
                .ToList();
        }
    }
}