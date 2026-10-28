using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDayAPI.Helpers;

namespace RaceDayAPI.Filters;

// [SessionAuthorize]                    = any logged in user
// [SessionAuthorize(Roles.Organiser)]   = organisers only
// [SessionAuthorize(Roles.Participant)] = participants only
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _role;

    public SessionAuthorizeAttribute(string? role = null)
    {
        _role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.Role);

        // not logged in at all
        if (userId == null || role == null)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "You need to log in first." });
            return;
        }

        // logged in, but the wrong role for this endpoint
        if (_role != null && role != _role)
        {
            context.Result = new ObjectResult(new { message = $"This action is for {_role}s only." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}