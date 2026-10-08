using System.ComponentModel.DataAnnotations;

namespace CinemaManagement.MVC.ViewModels
{
    public class ShowtimeViewModel
    {
        public int ShowtimeID { get; set; }

        [Required(ErrorMessage = "Chọn phim")]
        public int MovieID { get; set; }
        public string? MovieTitle { get; set; }

        [Required(ErrorMessage = "Chọn phòng")]
        public int HallID { get; set; }
        public string? HallName { get; set; }

        [Required(ErrorMessage = "Chọn giờ chiếu")]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [Range(1000, 1000000, ErrorMessage = "Giá vé không hợp lệ")]
        public decimal BasePrice { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class SelectItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}