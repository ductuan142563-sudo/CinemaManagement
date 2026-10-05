namespace CinemaManagement.API.DTOs
{
    public class CreateBookingRequest
    {
        public int ShowtimeId { get; set; }
        public List<int> SeatIds { get; set; } = new();
        public int? MemberId { get; set; }
    }

    public class BookingResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? BookingCode { get; set; }
        public decimal FinalAmount { get; set; }
        public int PointsEarned { get; set; }
        public int TotalSeats { get; set; }
    }

    public class SeatStatusDto
    {
        public int SeatId { get; set; }
        public string RowLabel { get; set; } = string.Empty;
        public int SeatNumber { get; set; }
        public string SeatType { get; set; } = "Standard";
        public string DisplayName => $"{RowLabel}{SeatNumber}";
        public bool IsBooked { get; set; }
    }

    public class ShowtimeSeatMapResponse
    {
        public int ShowtimeId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string HallName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public decimal BasePrice { get; set; }
        public List<SeatStatusDto> Seats { get; set; } = new();
    }
}