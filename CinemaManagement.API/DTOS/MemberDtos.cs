namespace CinemaManagement.API.DTOs
{
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    public class MemberProfileResponse
    {
        public int MemberId { get; set; }
        public string MembershipCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public string TierName { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public decimal PointRate { get; set; }

        public int CurrentPoints { get; set; }
        public int TotalPoints { get; set; }
        public DateTime JoinDate { get; set; }
    }

    public class PointTransactionDto
    {
        public int TransactionId { get; set; }
        public int Points { get; set; }
        public string Type { get; set; } = string.Empty;      // Earn / Redeem
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RedeemPointsRequest
    {
        public int MemberId { get; set; }
        public int PointsToRedeem { get; set; }
    }
}