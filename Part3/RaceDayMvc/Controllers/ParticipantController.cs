using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Filters;
using RaceDayMvc.Helpers;
using RaceDayMvc.Models;
using RaceDayMvc.Services;

namespace RaceDayMvc.Controllers;

// every page in here is for participants only
[RoleAuthorize(Roles.Participant)]
public class ParticipantController : BaseController
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly string[] ImageTypes = { "image/jpeg", "image/png", "image/webp" };

    public ParticipantController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Enrolments()
    {
        var result = await Api.GetAsync<List<EnrolmentItem>>("api/enrolments/me");
        var failure = HandleCommonFailure(result);
        if (failure != null) return failure;

        return View(result.Data ?? new List<EnrolmentItem>());
    }

    public async Task<IActionResult> Results()
    {
        var result = await Api.GetAsync<List<ResultItem>>("api/results/me");
        var failure = HandleCommonFailure(result);
        if (failure != null) return failure;

        return View(result.Data ?? new List<ResultItem>());
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var result = await Api.GetAsync<ProfileItem>("api/participants/me");
        var failure = HandleCommonFailure(result);
        if (failure != null) return failure;

        return View(new ParticipantProfileViewModel
        {
            Name = result.Data?.Name ?? string.Empty,
            Email = result.Data?.Email ?? string.Empty,
            ProfilePictureUrl = result.Data?.ProfilePictureUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ParticipantProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await Api.PutAsync<ProfileItem>("api/participants/me", new
        {
            name = model.Name.Trim(),
            email = model.Email.Trim()
        });

        var failure = HandleCommonFailure(result);
        if (failure != null) return failure;

        if (!result.Ok || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "We couldn't update your profile. Please try again.");
            return View(model);
        }

        // keep the name in the menu up to date
        HttpContext.Session.SetString(SessionKeys.UserName, result.Data.Name);

        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture == null || picture.Length == 0)
        {
            TempData["Error"] = "Please choose an image first.";
            return RedirectToAction(nameof(Profile));
        }

        // a quick friendly check before we send anything. the API checks again
        if (picture.Length > MaxImageBytes)
        {
            TempData["Error"] = "That image is too big. The limit is 5 MB.";
            return RedirectToAction(nameof(Profile));
        }

        if (!ImageTypes.Contains(picture.ContentType.ToLowerInvariant()))
        {
            TempData["Error"] = "Only JPG, PNG or WEBP images are allowed.";
            return RedirectToAction(nameof(Profile));
        }

        // the file goes to the API, and the API is the one that stores it in Azure
        var result = await Api.UploadAsync<ImageUploadItem>("api/participants/me/picture", picture);
        var failure = HandleCommonFailure(result);
        if (failure != null) return failure;

        if (result.Ok)
            TempData["Success"] = "Your profile picture has been updated.";
        else
            TempData["Error"] = result.Message ?? "We couldn't upload that picture. Please try again.";

        return RedirectToAction(nameof(Profile));
    }
}