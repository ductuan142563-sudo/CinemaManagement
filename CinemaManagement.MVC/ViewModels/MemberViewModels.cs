using System.ComponentModel.DataAnnotations;

namespace CinemaManagement.MVC.ViewModels;

/// <summary>Khớp response của GET api/member/profile/{memberId}.</summary>
public class MemberProfileViewModel
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

/// <summary>Khớp từng phần tử của GET api/member/points/{memberId}.</summary>
public class PointTransactionViewModel
{
    public int TransactionId { get; set; }
    public int Points { get; set; }                 // dương = cộng điểm, âm = trừ điểm
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Body gửi lên POST api/member/redeem.</summary>
public class RedeemRequest
{
    public int MemberId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Số điểm đổi phải lớn hơn 0")]
    public int PointsToRedeem { get; set; }
}

/// <summary>Response của POST api/member/redeem (lỗi trả về { message }).</summary>
public class RedeemResultViewModel
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>Model cho trang Profile và PointHistory.</summary>
public class MemberPageViewModel
{
    public int? MemberId { get; set; }              // null = Admin chưa nhập mã thành viên
    public MemberProfileViewModel? Profile { get; set; }
    public List<PointTransactionViewModel> Transactions { get; set; } = new();
}