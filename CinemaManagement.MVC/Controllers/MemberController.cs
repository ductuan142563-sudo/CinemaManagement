using CinemaManagement.MVC.Helpers;   // chỉnh namespace nếu [AuthorizeSession] của PART 2 nằm chỗ khác
using CinemaManagement.MVC.Services;
using CinemaManagement.MVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CinemaManagement.MVC.Controllers;

[AuthorizeSession]
public class MemberController : Controller
{
    private readonly ApiService _api;

    public MemberController(ApiService api)
    {
        _api = api;
    }

    // ---------- Hồ sơ + 20 giao dịch điểm mới nhất + form đổi điểm ----------
    [HttpGet]
    public async Task<IActionResult> Profile(int? id)
    {
        var memberId = ResolveMemberId(id);

        // Admin/Staff mở menu "Thành viên" mà chưa nhập mã -> hiện form tra cứu
        if (memberId == null)
            return View(new MemberPageViewModel());

        var vm = new MemberPageViewModel { MemberId = memberId };

        try
        {
            vm.Profile = await _api.GetAsync<MemberProfileViewModel>($"api/member/profile/{memberId}");
            if (vm.Profile == null)
            {
                TempData["Error"] = "Không tìm thấy thành viên.";
                return View(vm);
            }

            var all = await _api.GetAsync<List<PointTransactionViewModel>>($"api/member/points/{memberId}") ?? new();
            vm.Transactions = all.OrderByDescending(t => t.CreatedAt).Take(20).ToList();
        }
        catch (Exception)
        {
            TempData["Error"] = "Không tải được hồ sơ thành viên. Vui lòng thử lại.";
        }

        return View(vm);
    }

    // ---------- Toàn bộ lịch sử điểm ----------
    [HttpGet]
    public async Task<IActionResult> PointHistory(int? id)
    {
        var memberId = ResolveMemberId(id);
        if (memberId == null)
            return RedirectToAction(nameof(Profile));

        var vm = new MemberPageViewModel { MemberId = memberId };

        try
        {
            var all = await _api.GetAsync<List<PointTransactionViewModel>>($"api/member/points/{memberId}") ?? new();
            vm.Transactions = all.OrderByDescending(t => t.CreatedAt).ToList();
        }
        catch (Exception)
        {
            TempData["Error"] = "Không tải được lịch sử điểm. Vui lòng thử lại.";
        }

        return View(vm);
    }

    // ---------- Đổi điểm ----------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Redeem(int? memberId, int pointsToRedeem)
    {
        // Member luôn dùng MemberId trong Session, không tin giá trị từ form
        var id = ResolveMemberId(memberId);
        if (id == null)
        {
            TempData["Error"] = "Không xác định được thành viên.";
            return RedirectToAction(nameof(Profile));
        }

        if (pointsToRedeem <= 0)
        {
            TempData["Error"] = "Số điểm đổi phải lớn hơn 0.";
            return RedirectToProfile(id.Value);
        }

        try
        {
            var request = new RedeemRequest { MemberId = id.Value, PointsToRedeem = pointsToRedeem };

            // PostAsync trả về tuple (Success, Data, Message)
            var result = await _api.PostAsync<RedeemResultViewModel>("api/member/redeem", request);

            if (result.Success)
            {
                TempData["Success"] = !string.IsNullOrWhiteSpace(result.Data?.Message)
                    ? result.Data!.Message
                    : $"Đã đổi {pointsToRedeem:N0} điểm thành công.";
            }
            else
            {
                // Ưu tiên thông báo của API (ví dụ: "Không đủ điểm...")
                TempData["Error"] = !string.IsNullOrWhiteSpace(result.Data?.Message) ? result.Data!.Message
                                  : !string.IsNullOrWhiteSpace(result.Message) ? result.Message
                                  : "Đổi điểm thất bại.";
            }
        }
        catch (Exception)
        {
            TempData["Error"] = "Có lỗi xảy ra khi đổi điểm. Vui lòng thử lại.";
        }

        return RedirectToProfile(id.Value);
    }

    // ---------- Hàm phụ ----------

    // Member: chỉ được xem hồ sơ của chính mình (lấy từ Session).
    // Admin/Staff: dùng id truyền vào (có thể null).
    private int? ResolveMemberId(int? requestedId)
    {
        var role = HttpContext.Session.GetString("Role");

        if (role == "Member")
            return HttpContext.Session.GetMemberIdOrNull();

        if (role == "Admin" || role == "Staff")
            return requestedId;

        return null;
    }

    private IActionResult RedirectToProfile(int id) =>
        HttpContext.Session.GetString("Role") == "Member"
            ? RedirectToAction(nameof(Profile))
            : RedirectToAction(nameof(Profile), new { id });
}