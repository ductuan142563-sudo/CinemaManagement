namespace CinemaManagement.API.DTOs
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        // Thông tin Member (nếu có)
        public int? MemberID { get; set; }
        public string? MembershipCode { get; set; }
        public string? TierName { get; set; }
        public int? CurrentPoints { get; set; }

        public static AuthResponse Ok(
            int userId, string username, string fullName, string role,
            int? memberId = null, string? membershipCode = null,
            string? tierName = null, int? currentPoints = null,
            string message = "Đăng nhập thành công")
        {
            return new AuthResponse
            {
                Success = true,
                Message = message,
                UserID = userId,
                Username = username,
                FullName = fullName,
                Role = role,
                MemberID = memberId,
                MembershipCode = membershipCode,
                TierName = tierName,
                CurrentPoints = currentPoints
            };
        }

        public static AuthResponse Fail(string message)
            => new() { Success = false, Message = message };
    }
}