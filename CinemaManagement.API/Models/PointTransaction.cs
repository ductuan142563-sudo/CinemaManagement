using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.API.Models
{
    public class PointTransaction
    {
        [Key]
        public int TransactionID { get; set; }

        public int MemberID { get; set; }

        public int Points { get; set; }

        [MaxLength(20)]
        public string Type { get; set; } = "Earn"; // Earn | Redeem | Adjust

        [MaxLength(255)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(MemberID))]
        public Member Member { get; set; } = null!;
    }
}