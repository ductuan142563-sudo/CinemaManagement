using System.ComponentModel.DataAnnotations;
using CinemaManagement.MVC.Helpers;

namespace CinemaManagement.MVC.ViewModels;

/// <summary>Khớp từng phần tử của GET api/concession.</summary>
public class ConcessionItemViewModel
{
    public int ConcessionItemID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // Màu ô minh họa theo nhóm món
    public int Hue => Category switch
    {
        "Combo" => 8,
        "Bắp" => 38,
        "Nước" => 200,
        _ => 265
    };
}

public class ConcessionMenuViewModel
{
    public List<ConcessionItemViewModel> Items { get; set; } = new();
    public bool LoadFailed { get; set; }
    public bool QrEnabled { get; set; }   // đã cấu hình tài khoản nhận tiền cho thanh toán QR
}

/// <summary>Một món trong trang quản lý: vừa là dòng trong danh sách, vừa là dữ liệu của form thêm/sửa.</summary>
public class ConcessionAdminItemViewModel
{
    public int ConcessionItemID { get; set; }

    [Required(ErrorMessage = "Nhập tên món")]
    [MaxLength(100, ErrorMessage = "Tên món tối đa 100 ký tự")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300, ErrorMessage = "Mô tả tối đa 300 ký tự")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Chọn nhóm món")]
    public string Category { get; set; } = "Bắp";

    [Range(1000, 1000000, ErrorMessage = "Giá phải từ 1.000đ đến 1.000.000đ")]
    public decimal Price { get; set; } = 40000;

    public bool IsActive { get; set; } = true;
}

public class ConcessionAdminPageViewModel
{
    public List<ConcessionAdminItemViewModel> Items { get; set; } = new();
    public string? LoadError { get; set; }
}

public class FoodOrderItemViewModel
{
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class FoodOrderViewModel
{
    public int FoodOrderID { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string? Note { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "COD";
    public string PaymentStatus { get; set; } = "Pending";
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<FoodOrderItemViewModel> Items { get; set; } = new();

    // Tình trạng nhận món
    public string StatusText => Status switch
    {
        "Confirmed" => "Chờ nhận món",
        "Completed" => "Đã giao",
        "Cancelled" => "Đã hủy",
        _ => Status
    };

    public string StatusCss => Status switch
    {
        "Confirmed" => "status-chip--info",
        "Completed" => "status-chip--done",
        "Cancelled" => "status-chip--cancel",
        _ => "status-chip--done"
    };

    // Thanh toán
    public string PaymentMethodText => PaymentMethod == "QR" ? "Quét mã QR (chuyển khoản)" : "Tiền mặt (COD)";
    public string PaymentStatusText => PaymentStatus == "Paid" ? "Đã thanh toán" : "Chưa thanh toán";
    public string PaymentCss => PaymentStatus == "Paid" ? "status-chip--live" : "status-chip--soon";
}

/// <summary>Đơn kèm thông tin khách (dùng cho trang xử lý của nhân viên).</summary>
public class FoodOrderManageViewModel : FoodOrderViewModel
{
    public string CustomerName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

public class FoodOrderManagePageViewModel
{
    public List<FoodOrderManageViewModel> Open { get; set; } = new();   // đang chờ nhận món
    public List<FoodOrderManageViewModel> Done { get; set; } = new();   // đã giao hoặc đã hủy
    public string? LoadError { get; set; }
}

public class FoodOrdersPageViewModel
{
    public List<FoodOrderViewModel> Orders { get; set; } = new();
    public bool LoadFailed { get; set; }
}

/// <summary>Trang chi tiết một đơn, kèm mã QR nếu đang chờ thanh toán QR.</summary>
public class FoodOrderDetailsViewModel
{
    public FoodOrderViewModel Order { get; set; } = new();
    public PaymentQrInfo? Qr { get; set; }
}

/// <summary>Khớp response của POST api/concession/orders.</summary>
public class FoodOrderResultViewModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? OrderCode { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentMethod { get; set; }
}