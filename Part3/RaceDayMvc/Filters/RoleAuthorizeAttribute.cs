using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDayMvc.Helpers;

namespace RaceDayMvc.Filters;

// [RoleAuthorize]                    = any logged in user
// [RoleAuthorize(Roles.Organiser)]   = organisers only
// [RoleAuthorize(Roles.Participant)] = participants only
public class RoleAuthorizeAttribute : ActionFilterAttribute
{
    private readonly string? _role;

    public RoleAuthorizeAttribute(string? role = null)
    {
        _role = role;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;
        var session = context.HttpContext.Session;

        // not logged in: send them to the login page
        if (!session.IsLoggedIn())
        {
            if (context.Controller is Controller controller)
                controller.TempData["Info"] = "Please log in to continue.";

            // remember the page they wanted, but only for GET pages (a form post can't be replayed)
            object? routeValues = request.Method == HttpMethods.Get
                ? new { returnUrl = request.Path + request.QueryString }
                : null;

            context.Result = new RedirectToActionResult("Login", "Account", routeValues);
            return;
        }

        // logged in, but this page is for the other role
        if (_role != null && session.GetRole() != _role)
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}