using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.API.Models
{
    public class Seat
    {
        [Key]
        public int SeatID { get; set; }

        public int HallID { get; set; }

        [MaxLength(5)]
        public string RowLabel { get; set; } = string.Empty;

        public int SeatNumber { get; set; }

        [MaxLength(20)]
        public string SeatType { get; set; } = "Standard";

        [ForeignKey(nameof(HallID))]
        public Hall Hall { get; set; } = null!;
    }
}