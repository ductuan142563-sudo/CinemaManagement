using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CinemaManagement.MVC.Helpers
{
    public class AuthorizeSessionAttribute : ActionFilterAttribute
    {
        public string[]? Roles { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            if (!SessionHelper.IsLoggedIn(session))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (Roles != null && Roles.Length > 0)
            {
                var role = SessionHelper.GetRole(session);
                if (role == null || !Roles.Contains(role))
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}