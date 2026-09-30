using CinemaManagement.API.Data;
using CinemaManagement.API.DTOs;
using CinemaManagement.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly CinemaDbContext _context;

        public AuthController(CinemaDbContext context)
        {
            _context = context;
            _authService = new AuthService(context);
        }

        /// <summary>
        /// POST /api/auth/login
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(AuthResponse.Fail("Username và Password không được để trống."));

            try
            {
                var user = _authService.Login(request.Username.Trim(), request.Password);

                if (user == null)
                    return Unauthorized(AuthResponse.Fail("Sai tên đăng nhập hoặc mật khẩu."));

                // Load Member + Tier nếu là Member
                int? memberId = null;
                string? membershipCode = null;
                string? tierName = null;
                int? currentPoints = null;

                if (user.Role == "Member")
                {
                    var member = _context.Members
                        .Include(m => m.Tier)
                        .FirstOrDefault(m => m.UserID == user.UserID);

                    if (member != null)
                    {
                        memberId = member.MemberID;
                        membershipCode = member.MembershipCode;
                        tierName = member.Tier?.TierName;
                        currentPoints = member.CurrentPoints;
                    }
                }

                var response = AuthResponse.Ok(
                    user.UserID,
                    user.Username,
                    user.FullName ?? user.Username,
                    user.Role,
                    memberId,
                    membershipCode,
                    tierName,
                    currentPoints
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, AuthResponse.Fail($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        /// <summary>
        /// POST /api/auth/register
        /// </summary>
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FullName))
            {
                return BadRequest(AuthResponse.Fail("Username, Password và FullName là bắt buộc."));
            }

            try
            {
                bool success = _authService.Register(
                    request.Username.Trim(),
                    request.Password,
                    request.FullName.Trim(),
                    request.Email?.Trim(),
                    request.Phone?.Trim()
                );

                if (!success)
                    return BadRequest(AuthResponse.Fail("Username đã tồn tại hoặc đăng ký thất bại."));

                return Ok(AuthResponse.Ok(
                    0, request.Username, request.FullName, "Member",
                    message: "Đăng ký thành công! Bạn có thể đăng nhập."
                ));
            }
            catch (Exception ex)
            {
                return StatusCode(500, AuthResponse.Fail($"Lỗi hệ thống: {ex.Message}"));
            }
        }

        /// <summary>
        /// GET /api/auth/me?userId=1  (lấy thông tin user hiện tại – đơn giản)
        /// </summary>
        [HttpGet("me")]
        public IActionResult GetCurrentUser([FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest(AuthResponse.Fail("userId không hợp lệ."));

            try
            {
                var user = _context.Users.FirstOrDefault(u => u.UserID == userId && u.IsActive);

                if (user == null)
                    return NotFound(AuthResponse.Fail("Không tìm thấy user."));

                int? memberId = null;
                string? membershipCode = null;
                string? tierName = null;
                int? currentPoints = null;

                if (user.Role == "Member")
                {
                    var member = _context.Members
                        .Include(m => m.Tier)
                        .FirstOrDefault(m => m.UserID == user.UserID);

                    if (member != null)
                    {
                        memberId = member.MemberID;
                        membershipCode = member.MembershipCode;
                        tierName = member.Tier?.TierName;
                        currentPoints = member.CurrentPoints;
                    }
                }

                return Ok(AuthResponse.Ok(
                    user.UserID,
                    user.Username,
                    user.FullName ?? user.Username,
                    user.Role,
                    memberId,
                    membershipCode,
                    tierName,
                    currentPoints
                ));
            }
            catch (Exception ex)
            {
                return StatusCode(500, AuthResponse.Fail($"Lỗi hệ thống: {ex.Message}"));
            }
        }
    }
}