using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.API.Models
{
    public class Showtime
    {
        [Key]
        public int ShowtimeID { get; set; }

        public int MovieID { get; set; }

        public int HallID { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public decimal BasePrice { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(MovieID))]
        public Movie Movie { get; set; } = null!;

        [ForeignKey(nameof(HallID))]
        public Hall Hall { get; set; } = null!;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}