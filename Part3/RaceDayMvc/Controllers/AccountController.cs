using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Filters;
using RaceDayMvc.Helpers;
using RaceDayMvc.Models;
using RaceDayMvc.Services;

namespace RaceDayMvc.Controllers;

public class AccountController : BaseController
{
    public AccountController(ApiClient api) : base(api) { }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.IsLoggedIn())
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // start from a clean session, the API call below stores the API's session cookie in it
        HttpContext.Session.Clear();

        var result = await Api.LoginAsync(model.Email.Trim(), model.Password);
        if (!result.Ok || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "That didn't work. Please try again.");
            return View(model);
        }

        // remember who this is and what they're allowed to do, on every page from now on
        HttpContext.Session.SetInt32(SessionKeys.UserId, result.Data.UserId);
        HttpContext.Session.SetString(SessionKeys.UserName, result.Data.Name);
        HttpContext.Session.SetString(SessionKeys.Role, result.Data.Role);

        TempData["Success"] = $"Welcome back, {result.Data.Name}!";

        // only follow return links that stay on this site
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.IsLoggedIn())
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role == Roles.Organiser && string.IsNullOrWhiteSpace(model.OrganiserAccessCode))
            ModelState.AddModelError(nameof(model.OrganiserAccessCode), "Organisers need an access code.");

        if (!ModelState.IsValid)
            return View(model);

        var result = await Api.PostAsync<AuthUser>("api/auth/register", new
        {
            name = model.Name.Trim(),
            email = model.Email.Trim(),
            password = model.Password,
            role = model.Role,
            organiserAccessCode = model.Role == Roles.Organiser ? model.OrganiserAccessCode : null
        });

        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "We couldn't create your account. Please try again.");
            return View(model);
        }

        TempData["Success"] = "Your account is ready. Log in to get started.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // tell the API too, but clear our own session even if that call fails
        if (HttpContext.Session.IsLoggedIn())
            await Api.PostAsync("api/auth/logout");

        HttpContext.Session.Clear();
        TempData["Success"] = "You've been logged out.";
        return RedirectToAction(nameof(Login));
    }

    [RoleAuthorize]
    public IActionResult AccessDenied() => View();
}