using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.API.Models
{
    public class Booking
    {
        [Key]
        public int BookingID { get; set; }

        public int? MemberID { get; set; }

        public int ShowtimeID { get; set; }

        [Required, MaxLength(20)]
        public string BookingCode { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal FinalAmount { get; set; }

        public int PointsEarned { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(MemberID))]
        public Member? Member { get; set; }

        [ForeignKey(nameof(ShowtimeID))]
        public Showtime Showtime { get; set; } = null!;

        public ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();
    }
}