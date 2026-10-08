using System.ComponentModel.DataAnnotations;

namespace CinemaManagement.MVC.ViewModels
{
    public class MovieViewModel
    {
        public int MovieID { get; set; }

        [Required(ErrorMessage = "Tên phim không được để trống")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Director { get; set; }

        [Range(1, 500, ErrorMessage = "Thời lượng phải từ 1–500 phút")]
        public int Duration { get; set; }

        public int? GenreID { get; set; }
        public string? GenreName { get; set; }

        public string? AgeRating { get; set; }
        public string? Description { get; set; }
        public string? PosterUrl { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class GenreItem
    {
        public int GenreID { get; set; }
        public string GenreName { get; set; } = string.Empty;
    }
}