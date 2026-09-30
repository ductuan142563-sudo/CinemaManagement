namespace CinemaManagement.API.DTOs
{
    public class MovieDto
    {
        public int MovieID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Director { get; set; }
        public int Duration { get; set; }          // phút
        public int? GenreID { get; set; }
        public string? GenreName { get; set; }
        public string? AgeRating { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class MovieCreateRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Director { get; set; }
        public int Duration { get; set; }
        public int? GenreID { get; set; }
        public string? AgeRating { get; set; }
        public string? Description { get; set; }
    }

    public class MovieUpdateRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Director { get; set; }
        public int Duration { get; set; }
        public int? GenreID { get; set; }
        public string? AgeRating { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}