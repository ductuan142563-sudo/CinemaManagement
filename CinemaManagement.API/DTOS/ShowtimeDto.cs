namespace CinemaManagement.API.DTOs
{
    public class ShowtimeDto
    {
        public int ShowtimeID { get; set; }
        public int MovieID { get; set; }
        public string? MovieTitle { get; set; }
        public int HallID { get; set; }
        public string? HallName { get; set; }
        public DateTime? StartTime { get; set; }  
        public DateTime? EndTime { get; set; }     
        public decimal BasePrice { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ShowtimeCreateRequest
    {
        public int MovieID { get; set; }
        public int HallID { get; set; }
        public DateTime StartTime { get; set; }   
        public decimal BasePrice { get; set; }
    }

    public class ShowtimeUpdateRequest
    {
        public int MovieID { get; set; }
        public int HallID { get; set; }
        public DateTime StartTime { get; set; }
        public decimal BasePrice { get; set; }
        public bool IsActive { get; set; } = true;
    }
}