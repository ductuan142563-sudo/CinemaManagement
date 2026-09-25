using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.API.Models
{
    public class Movie
    {
        [Key]
        public int MovieID { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Director { get; set; }

        public int Duration { get; set; }

        public DateTime? ReleaseDate { get; set; }

        public int? GenreID { get; set; }

        [MaxLength(500)]
        public string? PosterUrl { get; set; }

        public string? Description { get; set; }

        [MaxLength(10)]
        public string? AgeRating { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(GenreID))]
        public Genre? Genre { get; set; }

        public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
    }
}