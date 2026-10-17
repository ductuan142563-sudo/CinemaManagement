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