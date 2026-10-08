namespace CinemaManagement.API.DTOs
{
    public class RevenueByDateDto
    {
        public DateTime Date { get; set; }
        public int TotalBookings { get; set; }
        public int TotalTickets { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class TopMovieDto
    {
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public int TotalTickets { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class ReportFilterRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
}