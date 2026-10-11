using System.Globalization;

namespace CinemaManagement.MVC.ViewModels;

/// <summary>Định dạng số/tiền kiểu Việt Nam (100.000đ), không phụ thuộc culture của máy.</summary>
public static class ReportFormat
{
    private static readonly CultureInfo Vi = new("vi-VN");

    public static string Money(decimal value) => value.ToString("N0", Vi) + "đ";
    public static string Number(int value) => value.ToString("N0", Vi);
}

/// <summary>Khớp response của GET api/report/summary.</summary>
public class ReportSummaryViewModel
{
    public decimal TotalRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public int TotalTickets { get; set; }
    public int TotalMembers { get; set; }
}

/// <summary>Khớp từng phần tử của GET api/report/revenue.</summary>
public class DailyRevenueViewModel
{
    public DateTime Date { get; set; }
    public int TotalBookings { get; set; }
    public int TotalTickets { get; set; }
    public decimal TotalRevenue { get; set; }
}

/// <summary>Khớp từng phần tử của GET api/report/top-movies.</summary>
public class TopMovieViewModel
{
    public int MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public int TotalTickets { get; set; }
    public decimal TotalRevenue { get; set; }
}

/// <summary>Model cho trang Revenue.</summary>
public class RevenueReportViewModel
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<DailyRevenueViewModel> Items { get; set; } = new();

    public decimal SumRevenue => Items.Sum(i => i.TotalRevenue);
    public int SumTickets => Items.Sum(i => i.TotalTickets);
    public int SumBookings => Items.Sum(i => i.TotalBookings);
}

/// <summary>Model cho trang TopMovies.</summary>
public class TopMoviesReportViewModel
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int Top { get; set; } = 5;
    public List<TopMovieViewModel> Items { get; set; } = new();
}