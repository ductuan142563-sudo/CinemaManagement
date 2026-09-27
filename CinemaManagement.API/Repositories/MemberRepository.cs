using CinemaManagement.API.Data;
using CinemaManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.DAL.Repositories
{
    public class MemberRepository : GenericRepository<Member>
    {
        public MemberRepository(CinemaDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Lấy Member theo UserID, Include Tier + User
        /// </summary>
        public Member? GetByUserId(int userId)
        {
            return _dbSet
                .Include(m => m.Tier)
                .Include(m => m.User)
                .FirstOrDefault(m => m.UserID == userId);
        }

        /// <summary>
        /// Lấy Member theo MembershipCode
        /// </summary>
        public Member? GetByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            return _dbSet
                .Include(m => m.Tier)
                .Include(m => m.User)
                .FirstOrDefault(m => m.MembershipCode == code);
        }

        /// <summary>
        /// Cộng điểm cho Member (CurrentPoints + TotalPoints)
        /// </summary>
        public void AddPoints(int memberId, int points)
        {
            if (points <= 0) return;

            var member = _dbSet.Find(memberId);
            if (member == null) return;

            member.CurrentPoints += points;
            member.TotalPoints += points;
            // Save() sẽ được gọi ở Service layer
        }

        /// <summary>
        /// Lấy Member kèm đầy đủ navigation (Tier, User, PointTransactions)
        /// </summary>
        public Member? GetByIdWithDetails(int memberId)
        {
            return _dbSet
                .Include(m => m.Tier)
                .Include(m => m.User)
                .Include(m => m.PointTransactions
                    .OrderByDescending(pt => pt.CreatedAt)   
                    .Take(20))
                .FirstOrDefault(m => m.MemberID == memberId);
        }
    }
}