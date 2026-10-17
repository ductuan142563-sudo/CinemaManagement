using CinemaManagement.API.Data;
using CinemaManagement.API.DTOs;
using CinemaManagement.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]   // phải đăng nhập mới xem thực đơn và đặt bắp nước
    public class ConcessionController : ControllerBase
    {
        private const int MaxPerItem = 10;
        private const int MaxTotalQuantity = 30;
        private const int MaxLines = 50;
        private const int MaxNoteLength = 200;

        private readonly CinemaDbContext _context;

        public ConcessionController(CinemaDbContext context)
        {
            _context = context;
        }

        /// <summary>GET /api/concession : thực đơn đang bán</summary>
        [HttpGet]
        public async Task<IActionResult> GetMenu()
        {
            var items = await _context.ConcessionItems
                .Where(i => i.IsActive)
                .OrderBy(i => i.Category == "Combo" ? 0 : (i.Category == "Bắp" ? 1 : 2))
                .ThenBy(i => i.Price)
                .Select(i => new ConcessionItemDto
                {
                    ConcessionItemID = i.ConcessionItemID,
                    Name = i.Name,
                    Description = i.Description,
                    Category = i.Category,
                    Price = i.Price
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>POST /api/concession/orders : đặt bắp nước</summary>
        [HttpPost("orders")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateFoodOrderRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            if (request?.Items == null || request.Items.Count == 0)
                return BadRequest(new { message = "Vui lòng chọn ít nhất một món." });

            if (request.Items.Count > MaxLines)
                return BadRequest(new { message = "Đơn có quá nhiều dòng món." });

            if (request.Items.Any(i => i.Quantity < 0 || i.Quantity > MaxPerItem))
                return BadRequest(new { message = $"Mỗi món đặt tối đa {MaxPerItem} phần." });

            // Gộp các dòng trùng món
            var lines = request.Items
                .Where(i => i.Quantity > 0)
                .GroupBy(i => i.ConcessionItemID)
                .Select(g => new { ConcessionItemID = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToList();

            if (lines.Count == 0)
                return BadRequest(new { message = "Vui lòng chọn ít nhất một món." });

            if (lines.Any(l => l.Quantity > MaxPerItem))
                return BadRequest(new { message = $"Mỗi món đặt tối đa {MaxPerItem} phần." });

            if (lines.Sum(l => l.Quantity) > MaxTotalQuantity)
                return BadRequest(new { message = $"Mỗi đơn đặt tối đa {MaxTotalQuantity} phần." });

            var note = request.Note?.Trim();
            if (note != null && note.Length > MaxNoteLength)
                return BadRequest(new { message = $"Ghi chú tối đa {MaxNoteLength} ký tự." });

            // Giá lấy từ database, không tin giá do người dùng gửi lên
            var ids = lines.Select(l => l.ConcessionItemID).ToList();
            var items = await _context.ConcessionItems
                .Where(i => ids.Contains(i.ConcessionItemID) && i.IsActive)
                .ToListAsync();

            if (items.Count != ids.Count)
                return BadRequest(new { message = "Có món không còn bán. Vui lòng tải lại thực đơn." });

            var order = new FoodOrder
            {
                OrderCode = NewOrderCode(),
                UserID = userId,
                Note = string.IsNullOrWhiteSpace(note) ? null : note,
                Status = "Confirmed",
                CreatedAt = DateTime.Now
            };

            foreach (var line in lines)
            {
                var item = items.First(i => i.ConcessionItemID == line.ConcessionItemID);
                order.Items.Add(new FoodOrderItem
                {
                    ConcessionItemID = item.ConcessionItemID,
                    ItemName = item.Name,
                    Quantity = line.Quantity,
                    UnitPrice = item.Price
                });
            }

            order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);

            _context.FoodOrders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Đặt bắp nước thành công.",
                orderCode = order.OrderCode,
                totalAmount = order.TotalAmount
            });
        }

        /// <summary>GET /api/concession/orders/mine : các đơn của tôi (30 đơn mới nhất)</summary>
        [HttpGet("orders/mine")]
        public async Task<IActionResult> GetMyOrders()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var orders = await _context.FoodOrders
                .Where(o => o.UserID == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(30)
                .Select(o => new FoodOrderDto
                {
                    FoodOrderID = o.FoodOrderID,
                    OrderCode = o.OrderCode,
                    Note = o.Note,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt,
                    Items = o.Items.Select(i => new FoodOrderItemDto
                    {
                        ItemName = i.ItemName,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList()
                })
                .ToListAsync();

            return Ok(orders);
        }

        // ---------- Hàm phụ ----------

        // userId nằm trong token đăng nhập (claim "userId")
        private bool TryGetUserId(out int userId) =>
            int.TryParse(User.FindFirst("userId")?.Value, out userId) && userId > 0;

        private static string NewOrderCode() =>
            $"FB{DateTime.Now:yyMMdd}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}";
    }
}