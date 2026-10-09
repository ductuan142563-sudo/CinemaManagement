using System.ComponentModel.DataAnnotations;

namespace CinemaManagement.MVC.ViewModels;

public class MovieOptionViewModel
{
    public int MovieId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
}

public class ShowtimeOptionViewModel
{
    public int ShowtimeId { get; set; }
    public int MovieId { get; set; }
    public string HallName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public decimal BasePrice { get; set; }
}

/// <summary>Khớp schema ghế của API: seatId, rowLabel, seatNumber, seatType, displayName, isBooked.</summary>
public class SeatViewModel
{
    public int SeatId { get; set; }
    public string RowLabel { get; set; } = string.Empty;
    public int SeatNumber { get; set; }
    public string SeatType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsBooked { get; set; }
}

public class SeatMapViewModel
{
    public int ShowtimeId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public string HallName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public decimal BasePrice { get; set; }
    public List<SeatViewModel> Seats { get; set; } = new();
}

public class MemberInfoViewModel
{
    public string MembershipCode { get; set; } = string.Empty;
    public string TierName { get; set; } = string.Empty;
    public int CurrentPoints { get; set; }
    public decimal DiscountPercent { get; set; }
}

/// <summary>Model cho trang SeatMap (phần khung; dữ liệu ghế load bằng fetch).</summary>
public class BookingPageViewModel
{
    public int ShowtimeId { get; set; }
    public bool IsMember { get; set; }
    public MemberInfoViewModel? Member { get; set; }
    public int VndPerPoint { get; set; }        // chỉ dùng để ƯỚC TÍNH điểm trên UI
}

public class CreateBookingRequest
{
    [Range(1, int.MaxValue)]
    public int ShowtimeId { get; set; }

    public int? MemberId { get; set; }

    [Required, MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất 1 ghế")]
    public List<int> SeatIds { get; set; } = new();
}

/// <summary>Khớp schema BookingResponse của API.</summary>
public class BookingResultViewModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string BookingCode { get; set; } = string.Empty;
    public decimal FinalAmount { get; set; }
    public int PointsEarned { get; set; }
    public int TotalSeats { get; set; }
}