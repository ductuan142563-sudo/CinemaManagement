namespace CinemaManagement.MVC.ViewModels
{
    public class BookingIndexViewModel
    {
        public List<MovieOption> Movies { get; set; } = new();
        public int? SelectedMovieId { get; set; }
        public int? SelectedShowtimeId { get; set; }
    }

    public class MovieOption
    {
        public int MovieID { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class ShowtimeOption
    {
        public int ShowtimeID { get; set; }
        public string Display { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public DateTime StartTime { get; set; }
    }

    public class SeatStatusVm
    {
        public int SeatId { get; set; }
        public string RowLabel { get; set; } = string.Empty;
        public int SeatNumber { get; set; }
        public string SeatType { get; set; } = "Standard";
        public string DisplayName { get; set; } = string.Empty;
        public bool IsBooked { get; set; }
    }

    public class SeatMapVm
    {
        public int ShowtimeId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string HallName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public decimal BasePrice { get; set; }
        public List<SeatStatusVm> Seats { get; set; } = new();
    }

    public class CreateBookingVm
    {
        public int ShowtimeId { get; set; }
        public List<int> SeatIds { get; set; } = new();
        public int? MemberId { get; set; }
    }
}