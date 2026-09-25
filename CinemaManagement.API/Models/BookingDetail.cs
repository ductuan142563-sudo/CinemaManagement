using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.API.Models
{
    public class BookingDetail
    {
        [Key]
        public int BookingDetailID { get; set; }

        public int BookingID { get; set; }

        public int SeatID { get; set; }

        public decimal Price { get; set; }

        [ForeignKey(nameof(BookingID))]
        public Booking Booking { get; set; } = null!;

        [ForeignKey(nameof(SeatID))]
        public Seat Seat { get; set; } = null!;
    }
}