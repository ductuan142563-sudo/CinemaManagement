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
    [Authorize]   // phải đăng nhập mới xem thực đơn và đặt bắp nước; quản lý món chỉ Admin; xử lý đơn Admin và Staff
    public class ConcessionController : ControllerBase
    {
        private const int MaxPerItem = 10;
        private const int MaxTotalQuantity = 30;
        private const int MaxLines = 50;
        private const int MaxNoteLength = 200;

        private const decimal MinPrice = 1000m;
        private const decimal MaxPrice = 1000000m;
        private static readonly string[] Categories = { "Combo", "Bắp", "Nước" };
        private static readonly string[] PaymentMethods = { "COD", "QR" };

        private readonly CinemaDbContext _context;

        public ConcessionController(CinemaDbContext context)
        {
            _context = context;
        }

        // =====================================================================
        //  DÀNH CHO KHÁCH
        // =====================================================================

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

        /// <summary>POST /api/concession/orders : đặt bắp nước (kèm phương thức thanh toán COD hoặc QR)</summary>
        [HttpPost("orders")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateFoodOrderRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            if (request?.Items == null || request.Items.Count == 0)
                return BadRequest(new { message = "Vui lòng chọn ít nhất một món." });

            var method = request.PaymentMethod?.Trim().ToUpperInvariant();
            if (method == null || !PaymentMethods.Contains(method))
                return BadRequest(new { message = "Vui lòng chọn phương thức thanh toán (tiền mặt hoặc quét mã QR)." });

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
                PaymentMethod = method,
                PaymentStatus = "Pending",
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
                totalAmount = order.TotalAmount,
                paymentMethod = order.PaymentMethod
            });
        }

        /// <summary>GET /api/concession/orders/mine : các đơn của tôi (30 đơn mới nhất)</summary>
        [HttpGet("orders/mine")]
        public async Task<IActionResult> GetMyOrders()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var orders = await _context.FoodOrders
                .Include(o => o.Items)
                .Where(o => o.UserID == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(30)
                .ToListAsync();

            return Ok(orders.Select(o => MapOrder<FoodOrderDto>(o)).ToList());
        }

        /// <summary>GET /api/concession/orders/FB261017-A3F9C : một đơn (chủ đơn, hoặc Admin và Staff)</summary>
        [HttpGet("orders/{code}")]
        public async Task<IActionResult> GetOrder(string code)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var order = await _context.FoodOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.OrderCode == code);

            var isStaff = User.IsInRole("Admin") || User.IsInRole("Staff");

            // Không để lộ việc đơn có tồn tại hay không với người không phải chủ đơn
            if (order == null || (order.UserID != userId && !isStaff))
                return NotFound(new { message = "Không tìm thấy đơn." });

            return Ok(MapOrder<FoodOrderDto>(order));
        }

        // =====================================================================
        //  XỬ LÝ ĐƠN (Admin và Staff)
        // =====================================================================

        /// <summary>GET /api/concession/orders/manage : 100 đơn mới nhất, kèm thông tin khách</summary>
        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("orders/manage")]
        public async Task<IActionResult> GetOrdersForStaff()
        {
            var orders = await _context.FoodOrders
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .Take(100)
                .ToListAsync();

            var userIds = orders.Select(o => o.UserID).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserID))
                .ToDictionaryAsync(u => u.UserID);

            var result = orders.Select(o =>
            {
                var dto = MapOrder<FoodOrderManageDto>(o);
                users.TryGetValue(o.UserID, out var user);
                dto.Username = user?.Username ?? string.Empty;
                dto.CustomerName = user?.FullName ?? user?.Username ?? string.Empty;
                return dto;
            }).ToList();

            return Ok(result);
        }

        /// <summary>PUT /api/concession/orders/{code}/confirm-payment : xác nhận đã nhận tiền</summary>
        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("orders/{code}/confirm-payment")]
        public async Task<IActionResult> ConfirmPayment(string code)
        {
            var order = await _context.FoodOrders.FirstOrDefaultAsync(o => o.OrderCode == code);
            if (order == null)
                return NotFound(new { message = "Không tìm thấy đơn." });

            if (order.Status == "Cancelled")
                return BadRequest(new { message = "Đơn đã bị hủy." });

            if (order.PaymentStatus == "Paid")
                return BadRequest(new { message = "Đơn này đã được xác nhận thanh toán." });

            order.PaymentStatus = "Paid";
            order.PaidAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xác nhận nhận tiền." });
        }

        /// <summary>PUT /api/concession/orders/{code}/complete : giao món cho khách</summary>
        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("orders/{code}/complete")]
        public async Task<IActionResult> CompleteOrder(string code)
        {
            var order = await _context.FoodOrders.FirstOrDefaultAsync(o => o.OrderCode == code);
            if (order == null)
                return NotFound(new { message = "Không tìm thấy đơn." });

            if (order.Status == "Cancelled")
                return BadRequest(new { message = "Đơn đã bị hủy." });

            if (order.Status == "Completed")
                return BadRequest(new { message = "Đơn này đã được giao." });

            var collectedCash = false;

            if (order.PaymentStatus != "Paid")
            {
                // Chuyển khoản QR phải được xác nhận đã nhận tiền trước khi giao món
                if (order.PaymentMethod == "QR")
                    return BadRequest(new
                    {
                        message = "Đơn thanh toán QR chưa được xác nhận nhận tiền. " +
                                  "Hãy kiểm tra tài khoản ngân hàng và xác nhận thanh toán trước khi giao món."
                    });

                // Tiền mặt (COD): thu tiền khi giao món
                order.PaymentStatus = "Paid";
                order.PaidAt = DateTime.Now;
                collectedCash = true;
            }

            order.Status = "Completed";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = collectedCash ? "Đã giao món và ghi nhận thu tiền mặt." : "Đã giao món."
            });
        }

        /// <summary>PUT /api/concession/orders/{code}/cancel : hủy đơn</summary>
        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("orders/{code}/cancel")]
        public async Task<IActionResult> CancelOrder(string code)
        {
            var order = await _context.FoodOrders.FirstOrDefaultAsync(o => o.OrderCode == code);
            if (order == null)
                return NotFound(new { message = "Không tìm thấy đơn." });

            if (order.Status == "Completed")
                return BadRequest(new { message = "Đơn đã giao, không thể hủy." });

            if (order.Status == "Cancelled")
                return BadRequest(new { message = "Đơn này đã được hủy trước đó." });

            var wasPaid = order.PaymentStatus == "Paid";

            order.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = wasPaid
                    ? "Đã hủy đơn. Đơn này đã thanh toán, hãy hoàn tiền cho khách bên ngoài hệ thống."
                    : "Đã hủy đơn."
            });
        }

        // =====================================================================
        //  QUẢN LÝ THỰC ĐƠN (chỉ Admin)
        // =====================================================================

        /// <summary>GET /api/concession/manage : tất cả món, kể cả món đã ẩn</summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("manage")]
        public async Task<IActionResult> GetAllForAdmin()
        {
            var items = await _context.ConcessionItems
                .OrderBy(i => i.Category == "Combo" ? 0 : (i.Category == "Bắp" ? 1 : 2))
                .ThenBy(i => i.Name)
                .Select(i => new ConcessionAdminDto
                {
                    ConcessionItemID = i.ConcessionItemID,
                    Name = i.Name,
                    Description = i.Description,
                    Category = i.Category,
                    Price = i.Price,
                    IsActive = i.IsActive
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>GET /api/concession/5 : một món (để sửa)</summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.ConcessionItems
                .Where(i => i.ConcessionItemID == id)
                .Select(i => new ConcessionAdminDto
                {
                    ConcessionItemID = i.ConcessionItemID,
                    Name = i.Name,
                    Description = i.Description,
                    Category = i.Category,
                    Price = i.Price,
                    IsActive = i.IsActive
                })
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { message = "Không tìm thấy món." });

            return Ok(item);
        }

        /// <summary>POST /api/concession : thêm món mới</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateItem([FromBody] ConcessionItemRequest request)
        {
            var error = ValidateItem(request);
            if (error != null)
                return BadRequest(new { message = error });

            var name = request.Name.Trim();

            if (await _context.ConcessionItems.AnyAsync(i => i.Name == name))
                return BadRequest(new { message = "Đã có món trùng tên này trong thực đơn." });

            var item = new ConcessionItem
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Category = request.Category,
                Price = request.Price,
                IsActive = request.IsActive
            };

            _context.ConcessionItems.Add(item);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Thêm món thành công.",
                concessionItemID = item.ConcessionItemID
            });
        }

        /// <summary>PUT /api/concession/5 : sửa món</summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateItem(int id, [FromBody] ConcessionItemRequest request)
        {
            var item = await _context.ConcessionItems.FindAsync(id);
            if (item == null)
                return NotFound(new { message = "Không tìm thấy món." });

            var error = ValidateItem(request);
            if (error != null)
                return BadRequest(new { message = error });

            var name = request.Name.Trim();

            if (await _context.ConcessionItems.AnyAsync(i => i.Name == name && i.ConcessionItemID != id))
                return BadRequest(new { message = "Đã có món trùng tên này trong thực đơn." });

            item.Name = name;
            item.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            item.Category = request.Category;
            item.Price = request.Price;
            item.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật món thành công." });
        }

        /// <summary>PUT /api/concession/5/active : ẩn hoặc hiện lại món (không xóa hẳn vì đơn cũ còn tham chiếu)</summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}/active")]
        public async Task<IActionResult> SetActive(int id, [FromBody] ConcessionActiveRequest request)
        {
            var item = await _context.ConcessionItems.FindAsync(id);
            if (item == null)
                return NotFound(new { message = "Không tìm thấy món." });

            item.IsActive = request.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { message = request.IsActive ? "Đã hiện lại món." : "Đã ẩn món." });
        }

        // ---------- Hàm phụ ----------

        private static T MapOrder<T>(FoodOrder o) where T : FoodOrderDto, new() => new T
        {
            FoodOrderID = o.FoodOrderID,
            OrderCode = o.OrderCode,
            Note = o.Note,
            TotalAmount = o.TotalAmount,
            Status = o.Status,
            PaymentMethod = o.PaymentMethod,
            PaymentStatus = o.PaymentStatus,
            PaidAt = o.PaidAt,
            CreatedAt = o.CreatedAt,
            Items = o.Items.Select(i => new FoodOrderItemDto
            {
                ItemName = i.ItemName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        private static string? ValidateItem(ConcessionItemRequest? request)
        {
            if (request == null)
                return "Dữ liệu không hợp lệ.";

            var name = request.Name?.Trim();
            if (string.IsNullOrEmpty(name))
                return "Tên món không được để trống.";
            if (name.Length > 100)
                return "Tên món tối đa 100 ký tự.";

            if (request.Description != null && request.Description.Trim().Length > 300)
                return "Mô tả tối đa 300 ký tự.";

            if (!Categories.Contains(request.Category))
                return "Nhóm món không hợp lệ (chỉ nhận Combo, Bắp hoặc Nước).";

            if (request.Price < MinPrice || request.Price > MaxPrice)
                return "Giá phải từ 1.000đ đến 1.000.000đ.";

            return null;
        }

        // userId nằm trong token đăng nhập (claim "userId")
        private bool TryGetUserId(out int userId) =>
            int.TryParse(User.FindFirst("userId")?.Value, out userId) && userId > 0;

        private static string NewOrderCode() =>
            $"FB{DateTime.Now:yyMMdd}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}";
    }
}