using CinemaManagement.API.Data;
using CinemaManagement.API.DTOs;
using CinemaManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Member")]   // Staff không được vào; riêng register không cần đăng nhập
    public class MemberController : ControllerBase
    {
        private readonly CinemaDbContext _context;
        private readonly AuthService _authService;
        private readonly MembershipService _membershipService;

        public MemberController(
            CinemaDbContext context,
            AuthService authService,
            MembershipService membershipService)
        {
            _context = context;
            _authService = authService;
            _membershipService = membershipService;
        }

        /// <summary>
        /// Đăng ký thành viên mới (không cần đăng nhập)
        /// POST: api/member/register
        /// </summary>
        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username và Password không được để trống." });
            }

            var success = _authService.Register(
                request.Username,
                request.Password,
                request.FullName,
                request.Email,
                request.Phone
            );

            if (!success)
                return BadRequest(new { message = "Username đã tồn tại." });

            return Ok(new { message = "Đăng ký thành viên thành công!" });
        }

        /// <summary>
        /// Xem hồ sơ thành viên theo MemberId
        /// GET: api/member/profile/5
        /// Admin xem được mọi thành viên; Member chỉ xem được hồ sơ của chính mình.
        /// </summary>
        [HttpGet("profile/{memberId}")]
        public async Task<ActionResult<MemberProfileResponse>> GetProfile(int memberId)
        {
            if (IsMemberAccessingOthers(memberId))
                return ForbidOthers();

            var member = await _context.Members
                .Include(m => m.User)
                .Include(m => m.Tier)
                .FirstOrDefaultAsync(m => m.MemberID == memberId);

            if (member == null)
                return NotFound(new { message = "Không tìm thấy thành viên." });

            var response = new MemberProfileResponse
            {
                MemberId = member.MemberID,
                MembershipCode = member.MembershipCode,
                FullName = member.User?.FullName ?? "",
                Username = member.User?.Username ?? "",
                Email = member.User?.Email ?? "",
                Phone = member.User?.Phone ?? "",
                TierName = member.Tier?.TierName ?? "",
                DiscountPercent = member.Tier?.DiscountPercent ?? 0,
                PointRate = member.Tier?.PointRate ?? 0,
                CurrentPoints = member.CurrentPoints,
                TotalPoints = member.TotalPoints,
                JoinDate = member.JoinDate
            };

            return Ok(response);
        }

        /// <summary>
        /// Xem lịch sử điểm (20 giao dịch mới nhất)
        /// GET: api/member/points/5
        /// Admin xem được mọi thành viên; Member chỉ xem được của chính mình.
        /// </summary>
        [HttpGet("points/{memberId}")]
        public async Task<ActionResult<List<PointTransactionDto>>> GetPointHistory(int memberId)
        {
            if (IsMemberAccessingOthers(memberId))
                return ForbidOthers();

            var exists = await _context.Members.AnyAsync(m => m.MemberID == memberId);
            if (!exists)
                return NotFound(new { message = "Không tìm thấy thành viên." });

            var history = await _context.PointTransactions
                .Where(pt => pt.MemberID == memberId)
                .OrderByDescending(pt => pt.CreatedAt)
                .Take(20)
                .Select(pt => new PointTransactionDto
                {
                    TransactionId = pt.TransactionID,
                    Points = pt.Points,
                    Type = pt.Type,
                    Description = pt.Description,
                    CreatedAt = pt.CreatedAt
                })
                .ToListAsync();

            return Ok(history);
        }

        /// <summary>
        /// Đổi điểm
        /// POST: api/member/redeem
        /// Member chỉ được đổi điểm của chính mình.
        /// </summary>
        [HttpPost("redeem")]
        public IActionResult RedeemPoints([FromBody] RedeemPointsRequest request)
        {
            if (IsMemberAccessingOthers(request.MemberId))
                return ForbidOthers();

            if (request.PointsToRedeem <= 0)
                return BadRequest(new { message = "Số điểm phải lớn hơn 0." });

            var success = _membershipService.RedeemPoints(request.MemberId, request.PointsToRedeem);

            if (!success)
                return BadRequest(new { message = "Không đủ điểm hoặc thành viên không tồn tại." });

            return Ok(new { message = $"Đã đổi thành công {request.PointsToRedeem} điểm." });
        }

        // ---------- Hàm phụ ----------

        // MemberId nằm trong token (claim "memberId")
        private int? GetTokenMemberId() =>
            int.TryParse(User.FindFirst("memberId")?.Value, out var id) ? id : null;

        // Member chỉ được thao tác trên MemberId của chính mình; Admin thì không bị giới hạn
        private bool IsMemberAccessingOthers(int memberId) =>
            User.IsInRole("Member") && GetTokenMemberId() != memberId;

        private ObjectResult ForbidOthers() =>
            StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Bạn chỉ được thao tác trên hồ sơ của chính mình." });
    }
}