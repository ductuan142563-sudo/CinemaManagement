using CinemaManagement.API.Data;                   
using CinemaManagement.API.Models;
using CinemaManagement.DAL.Repositories;

namespace CinemaManagement.API.Services             
{
    public class AuthService
    {
        private readonly CinemaDbContext _context;
        private readonly MemberRepository _memberRepo;

        public AuthService(CinemaDbContext context)
        {
            _context = context;
            _memberRepo = new MemberRepository(context);
        }

        /// <summary>
        /// Đăng nhập. Trả về User nếu đúng, null nếu sai.
        /// </summary>
        public User? Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            var user = _context.Users
                .FirstOrDefault(u => u.Username == username && u.IsActive);

            if (user == null)
                return null;

            // Xác thực mật khẩu bằng BCrypt
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            return user;
        }

        /// <summary>
        /// Đăng ký tài khoản mới + tự tạo Member (Tier = Silver).
        /// Trả về true nếu thành công, false nếu username đã tồn tại.
        /// </summary>
        public bool Register(string username, string password, string fullName, string? email, string? phone)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            // Kiểm tra username trùng
            if (_context.Users.Any(u => u.Username == username))
                return false;

            // Hash password
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // Tạo User
            var user = new User
            {
                Username = username.Trim(),
                PasswordHash = passwordHash,
                FullName = fullName?.Trim(),
                Email = email?.Trim(),
                Phone = phone?.Trim(),
                Role = "Member",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            _context.SaveChanges();   // cần UserID để tạo Member

            // Tạo MembershipCode (VD: MB20260927143022)
            string membershipCode = GenerateMembershipCode();

            // Lấy Tier Silver (TierID = 1 theo seed data)
            var silverTier = _context.MembershipTiers
                .FirstOrDefault(t => t.TierName == "Silver")
                ?? _context.MembershipTiers.OrderBy(t => t.MinPoints).First();

            var member = new Member
            {
                UserID = user.UserID,
                MembershipCode = membershipCode,
                TierID = silverTier.TierID,
                TotalPoints = 0,
                CurrentPoints = 0,
                JoinDate = DateTime.Now
            };

            _context.Members.Add(member);
            _context.SaveChanges();

            return true;
        }

        private string GenerateMembershipCode()
        {
            string code;
            do
            {
                code = "MB" + DateTime.Now.ToString("yyyyMMddHHmmss");
                if (_context.Members.Any(m => m.MembershipCode == code))
                {
                    code = "MB" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                }
            } while (_context.Members.Any(m => m.MembershipCode == code));

            return code;
        }
    }
}