using System.ComponentModel.DataAnnotations;

namespace CinemaManagement.API.Models
{
    /// <summary>Một món trong thực đơn bắp nước.</summary>
    public class ConcessionItem
    {
        [Key]
        public int ConcessionItemID { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        [Required, MaxLength(30)]
        public string Category { get; set; } = "Bắp";   // Combo | Bắp | Nước

        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;
    }
}