using System.ComponentModel.DataAnnotations;

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
    public DateTime CreatedAt { get; set; }
    public List<FoodOrderItemViewModel> Items { get; set; } = new();

    public string StatusText => Status switch
    {
        "Confirmed" => "Chờ nhận món",
        "Completed" => "Đã nhận",
        "Cancelled" => "Đã hủy",
        _ => Status
    };

    public string StatusCss => Status switch
    {
        "Confirmed" => "status-chip--live",
        "Completed" => "status-chip--done",
        "Cancelled" => "status-chip--cancel",
        _ => "status-chip--done"
    };
}

public class FoodOrdersPageViewModel
{
    public List<FoodOrderViewModel> Orders { get; set; } = new();
    public bool LoadFailed { get; set; }
}

/// <summary>Khớp response của POST api/concession/orders.</summary>
public class FoodOrderResultViewModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? OrderCode { get; set; }
    public decimal TotalAmount { get; set; }
}