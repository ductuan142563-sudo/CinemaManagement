using System.ComponentModel.DataAnnotations;

namespace CinemaManagement.API.Models
{
    public class MembershipTier
    {
        [Key]
        public int TierID { get; set; }

        [Required, MaxLength(50)]
        public string TierName { get; set; } = string.Empty;

        public int MinPoints { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal PointRate { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        public ICollection<Member> Members { get; set; } = new List<Member>();
    }
}