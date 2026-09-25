using System.ComponentModel.DataAnnotations;

namespace CinemaManagement.API.Models
{
    public class Hall
    {
        [Key]
        public int HallID { get; set; }

        [Required, MaxLength(50)]
        public string HallName { get; set; } = string.Empty;

        public int TotalSeats { get; set; }

        public int RowsCount { get; set; }

        public int SeatsPerRow { get; set; }

        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
        public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
    }
}