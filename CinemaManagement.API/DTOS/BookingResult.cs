using CinemaManagement.API.Models;

namespace CinemaManagement.BLL.DTOs
{
    public class BookingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Booking? Booking { get; set; }

        public static BookingResult Ok(Booking booking, string message = "Đặt vé thành công")
            => new() { Success = true, Message = message, Booking = booking };

        public static BookingResult Fail(string message)
            => new() { Success = false, Message = message };
    }
}