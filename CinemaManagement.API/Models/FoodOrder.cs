using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.API.Models
{
    /// <summary>Một đơn bắp nước.</summary>
    public class FoodOrder
    {
        [Key]
        public int FoodOrderID { get; set; }

        [Required, MaxLength(30)]
        public string OrderCode { get; set; } = string.Empty;

        // Lấy từ token đăng nhập (mọi vai trò đều có userId)
        public int UserID { get; set; }

        [MaxLength(200)]
        public string? Note { get; set; }

        public decimal TotalAmount { get; set; }

        // Tình trạng nhận món: Confirmed (chờ nhận) | Completed (đã giao) | Cancelled (đã hủy)
        [Required, MaxLength(20)]
        public string Status { get; set; } = "Confirmed";

        // Phương thức thanh toán: COD (tiền mặt) | QR (chuyển khoản)
        [Required, MaxLength(10)]
        public string PaymentMethod { get; set; } = "COD";

        // Tình trạng thanh toán: Pending (chưa) | Paid (đã)
        [Required, MaxLength(10)]
        public string PaymentStatus { get; set; } = "Pending";

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<FoodOrderItem> Items { get; set; } = new List<FoodOrderItem>();
    }

    /// <summary>Một dòng trong đơn (lưu lại tên và đơn giá tại thời điểm đặt).</summary>
    public class FoodOrderItem
    {
        [Key]
        public int FoodOrderItemID { get; set; }

        public int FoodOrderID { get; set; }

        public int ConcessionItemID { get; set; }

        [Required, MaxLength(100)]
        public string ItemName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        [ForeignKey(nameof(FoodOrderID))]
        public FoodOrder? Order { get; set; }

        [ForeignKey(nameof(ConcessionItemID))]
        public ConcessionItem? Item { get; set; }
    }
}