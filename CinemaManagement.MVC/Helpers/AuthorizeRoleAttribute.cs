using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CinemaManagement.MVC.Helpers;

/// <summary>
/// Chỉ cho phép các Role được liệt kê truy cập.
/// Chưa đăng nhập -> về trang Login. Sai Role -> trang "Không có quyền".
/// Ví dụ: [AuthorizeRole("Admin", "Staff")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public AuthorizeRoleAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (!SessionHelper.IsLoggedIn(session))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        var role = SessionHelper.GetRole(session) ?? string.Empty;

        if (!_roles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToActionResult("Denied", "Access", null);
        }
    }
}