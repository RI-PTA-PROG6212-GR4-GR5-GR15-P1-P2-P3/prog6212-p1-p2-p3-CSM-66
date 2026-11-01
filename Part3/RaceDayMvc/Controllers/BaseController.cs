using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Services;

namespace RaceDayMvc.Controllers;

public abstract class BaseController : Controller
{
    protected ApiClient Api { get; }

    protected BaseController(ApiClient api)
    {
        Api = api;
    }

    // handles the failures every page has in common (logged out, not allowed, missing, API down).
    // returns null when it's something the page should deal with itself, like a validation message
    protected IActionResult? HandleCommonFailure(ApiResult result)
    {
        if (result.IsUnauthorized)
        {
            HttpContext.Session.Clear();
            TempData["Info"] = "Your session has ended. Please log in again.";
            return RedirectToAction("Login", "Account");
        }

        if (result.IsForbidden)
            return RedirectToAction("AccessDenied", "Account");

        if (result.IsNotFound)
            return NotFound();

        if (result.IsServerProblem)
            return View("ApiError", result.Message);

        return null;
    }
}