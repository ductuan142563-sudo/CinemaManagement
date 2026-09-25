using System.ComponentModel.DataAnnotations;

namespace CinemaManagement.API.Models
{
    public class Genre
    {
        [Key]
        public int GenreID { get; set; }

        [Required, MaxLength(50)]
        public string GenreName { get; set; } = string.Empty;

        public ICollection<Movie> Movies { get; set; } = new List<Movie>();
    }
}