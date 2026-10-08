using System.ComponentModel.DataAnnotations;

namespace CinemaManagement.MVC.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username không được để trống")]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password không được để trống")]
        [MinLength(6, ErrorMessage = "Password tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;
    }
}