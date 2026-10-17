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

    public class FoodOrderItemRequest
    {
        public int ConcessionItemID { get; set; }
        public int Quantity { get; set; }
    }

    public class CreateFoodOrderRequest
    {
        public List<FoodOrderItemRequest> Items { get; set; } = new();
        public string? Note { get; set; }
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
        public DateTime CreatedAt { get; set; }
        public List<FoodOrderItemDto> Items { get; set; } = new();
    }
}