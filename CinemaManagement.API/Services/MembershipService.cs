using CinemaManagement.API.Data;
using CinemaManagement.API.Models;
using CinemaManagement.DAL.Repositories;

namespace CinemaManagement.API.Services
{
    public class MembershipService
    {
        private readonly CinemaDbContext _context;
        private readonly MemberRepository _memberRepo;

        public MembershipService(CinemaDbContext context)
        {
            _context = context;
            _memberRepo = new MemberRepository(context);
        }

        /// <summary>
        /// Tính điểm nhận được.
        /// Công thức: (finalAmount / 10000) * PointRate
        /// </summary>
        public int CalculatePoints(decimal finalAmount, MembershipTier tier)
        {
            if (finalAmount <= 0 || tier == null)
                return 0;

            return (int)(finalAmount / 10000m * tier.PointRate);
        }

        /// <summary>
        /// Tính số tiền được giảm giá.
        /// Công thức: baseAmount * DiscountPercent / 100
        /// </summary>
        public decimal CalculateDiscount(decimal baseAmount, MembershipTier tier)
        {
            if (baseAmount <= 0 || tier == null)
                return 0;

            return baseAmount * tier.DiscountPercent / 100m;
        }

        /// <summary>
        /// Cộng điểm + ghi PointTransaction + kiểm tra nâng hạng.
        /// </summary>
        public void AddPointsAndCheckUpgrade(int memberId, int points)
        {
            if (points <= 0) return;

            var member = _context.Members
                .FirstOrDefault(m => m.MemberID == memberId);

            if (member == null) return;

            // Cộng điểm
            member.CurrentPoints += points;
            member.TotalPoints += points;

            // Ghi lịch sử
            var transaction = new PointTransaction
            {
                MemberID = memberId,
                Points = points,
                Type = "Earn",
                Description = $"Earn {points} points from booking",
                CreatedAt = DateTime.Now   
            };
            _context.PointTransactions.Add(transaction);

            // Kiểm tra nâng hạng
            CheckAndUpgradeTier(member);

            _context.SaveChanges();
        }

        /// <summary>
        /// Đổi điểm. Trả về true nếu đủ điểm, false nếu không đủ.
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

            // Ghi lịch sử
            var transaction = new PointTransaction
            {
                MemberID = memberId,
                Points = pointsToRedeem,
                Type = "Redeem",
                Description = $"Redeem {pointsToRedeem} points",
                CreatedAt = DateTime.Now   
            };
            _context.PointTransactions.Add(transaction);

            _context.SaveChanges();
            return true;
        }

        /// <summary>
        /// Kiểm tra TotalPoints để nâng hạng: Silver → Gold → Platinum
        /// </summary>
        private void CheckAndUpgradeTier(Member member)
        {
            // Lấy tất cả tier sắp xếp theo MinPoints giảm dần
            var tiers = _context.MembershipTiers
                .OrderByDescending(t => t.MinPoints)
                .ToList();

            foreach (var tier in tiers)
            {
                if (member.TotalPoints >= tier.MinPoints)
                {
                    if (member.TierID != tier.TierID)
                    {
                        member.TierID = tier.TierID;
                    }
                    break; // đã tìm thấy tier cao nhất phù hợp
                }
            }
        }
    }
}