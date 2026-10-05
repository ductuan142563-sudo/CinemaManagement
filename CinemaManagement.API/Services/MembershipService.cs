using CinemaManagement.API.Data;          
using CinemaManagement.API.Models;        
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.API.Services   
{
    public class MembershipService
    {
        private readonly CinemaDbContext _context;

        public MembershipService(CinemaDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tính điểm nhận được: (finalAmount / 10000) * PointRate
        /// </summary>
        public int CalculatePoints(decimal finalAmount, MembershipTier tier)
        {
            if (finalAmount <= 0 || tier == null)
                return 0;

            return (int)(finalAmount / 10000m * tier.PointRate);
        }

        /// <summary>
        /// Tính số tiền được giảm: baseAmount * DiscountPercent / 100
        /// </summary>
        public decimal CalculateDiscount(decimal baseAmount, MembershipTier tier)
        {
            if (baseAmount <= 0 || tier == null)
                return 0;

            return baseAmount * tier.DiscountPercent / 100m;
        }

        /// <summary>
        /// Cộng điểm + ghi PointTransaction + kiểm tra nâng hạng
        /// </summary>
        public void AddPointsAndCheckUpgrade(int memberId, int points)
        {
            if (points <= 0) return;

            var member = _context.Members
                .Include(m => m.Tier)               // nên Include
                .FirstOrDefault(m => m.MemberID == memberId);

            if (member == null) return;

            // Cộng điểm
            member.CurrentPoints += points;
            member.TotalPoints += points;

            // Ghi lịch sử (điểm dương = Earn)
            _context.PointTransactions.Add(new PointTransaction
            {
                MemberID = memberId,
                Points = points,
                Type = "Earn",
                Description = $"Earn {points} points from booking",
                CreatedAt = DateTime.Now
            });

            // Kiểm tra nâng hạng
            CheckAndUpgradeTier(member);

            _context.SaveChanges();
        }

        /// <summary>
        /// Đổi điểm. Trả về true nếu đủ điểm.
        /// </summary>
        public bool RedeemPoints(int memberId, int pointsToRedeem)
        {
            if (pointsToRedeem <= 0) return false;

            var member = _context.Members
                .FirstOrDefault(m => m.MemberID == memberId);

            if (member == null || member.CurrentPoints < pointsToRedeem)
                return false;

            // Trừ điểm
            member.CurrentPoints -= pointsToRedeem;

            // Ghi lịch sử (điểm âm = Redeem)  ← quan trọng
            _context.PointTransactions.Add(new PointTransaction
            {
                MemberID = memberId,
                Points = -pointsToRedeem,           // số âm
                Type = "Redeem",
                Description = $"Redeem {pointsToRedeem} points",
                CreatedAt = DateTime.Now
            });

            _context.SaveChanges();
            return true;
        }

        /// <summary>
        /// Chỉ nâng hạng, không hạ hạng.
        /// </summary>
        private void CheckAndUpgradeTier(Member member)
        {
            // Lấy tier cao nhất mà member đủ điều kiện
            var suitableTier = _context.MembershipTiers
                .Where(t => member.TotalPoints >= t.MinPoints)
                .OrderByDescending(t => t.MinPoints)
                .FirstOrDefault();

            if (suitableTier != null && member.TierID != suitableTier.TierID)
            {
                // Chỉ nâng (MinPoints của tier mới phải cao hơn tier hiện tại)
                if (member.Tier == null || suitableTier.MinPoints > member.Tier.MinPoints)
                {
                    member.TierID = suitableTier.TierID;
                }
            }
        }
    }
}