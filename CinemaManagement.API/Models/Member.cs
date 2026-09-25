using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.API.Models
{
    public class Member
    {
        [Key]
        public int MemberID { get; set; }

        public int UserID { get; set; }

        [Required, MaxLength(20)]
        public string MembershipCode { get; set; } = string.Empty;

        public int TierID { get; set; }
        public int TotalPoints { get; set; }
        public int CurrentPoints { get; set; }
        public DateTime JoinDate { get; set; } = DateTime.Now;

        [ForeignKey(nameof(UserID))]
        public User User { get; set; } = null!;

        [ForeignKey(nameof(TierID))]
        public MembershipTier Tier { get; set; } = null!;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
    }
}