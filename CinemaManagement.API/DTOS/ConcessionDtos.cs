namespace CinemaManagement.API.DTOs
{
    public class ConcessionItemDto
    {
        public int ConcessionItemID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    /// <summary>Món trong thực đơn, kèm trạng thái (dùng cho trang quản lý của Admin).</summary>
    public class ConcessionAdminDto
    {
        public int ConcessionItemID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>Dữ liệu thêm hoặc sửa một món.</summary>
    public class ConcessionItemRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;   // Combo | Bắp | Nước
        public decimal Price { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ConcessionActiveRequest
    {
        public bool IsActive { get; set; }
    }

    public class FoodOrderItemRequest
    {
        public int ConcessionItemID { get; set; }
        public int Quantity { get; set; }
    }

    public class CreateFoodOrderRequest
    {
        public List<FoodOrderItemRequest> Items { get; set; } = new();
        public string? Note { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;   // COD | QR
    }

    public class FoodOrderItemDto
    {
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class FoodOrderDto
    {
        public int FoodOrderID { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string? Note { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<FoodOrderItemDto> Items { get; set; } = new();
    }

    /// <summary>Đơn kèm thông tin khách (dùng cho trang xử lý của nhân viên).</summary>
    public class FoodOrderManageDto : FoodOrderDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }
}