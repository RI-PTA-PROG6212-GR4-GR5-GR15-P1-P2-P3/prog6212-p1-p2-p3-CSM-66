using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Filters;
using RaceDayMvc.Helpers;
using RaceDayMvc.Models;
using RaceDayMvc.Services;

namespace RaceDayMvc.Controllers;

// every page in here is for organisers only
[RoleAuthorize(Roles.Organiser)]
public class OrganiserController : BaseController
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly string[] ImageTypes = { "image/jpeg", "image/png", "image/webp" };
    private static readonly Regex TimePattern = new(@"^([01]?\d|2[0-3]):[0-5]\d:[0-5]\d$");

    public OrganiserController(ApiClient api) : base(api) { }

    // ---------- dashboard ----------

    public async Task<IActionResult> Dashboard()
    {
        var result = await Api.GetAsync<List<EventItem>>("api/events/mine");
        var failure = HandleCommonFailure(result);
        if (failure != null) return failure;

        return View(new DashboardViewModel { Events = result.Data ?? new List<EventItem>() });
    }

    // ---------- events ----------

    public async Task<IActionResult> Events()
    {
        var result = await Api.GetAsync<List<EventItem>>("api/events/mine");
        var failure = HandleCommonFailure(result);
        if (failure != null) return failure;

        return View(result.Data ?? new List<EventItem>());
    }

    [HttpGet]
    public IActionResult CreateEvent() => View("EventForm", new EventFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        var bannerError = CheckBanner(model.BannerFile);
        if (bannerError != null)
            ModelState.AddModelError(nameof(model.BannerFile), bannerError);

        if (!ModelState.IsValid)
            return View("EventForm", model);

        var result = await Api.PostAsync<EventItem>("api/events", ToApiEvent(model));
        var failure = HandleCommonFailure(result);
        if (failure != null) return failure;

        if (!result.Ok || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "We couldn't save the event. Please try again.");
            return View("EventForm", model);
        }

        // the event exists now, so the banner goes up separately. if it fails the event is still saved
        var warning = await UploadBannerAsync(result.Data.EventId, model.BannerFile);
        if (warning == null)
            TempData["Success"] = "Your event has been created.";
        else
            TempData["Error"] = $"The event was saved, but the banner didn't upload: {warning} You can add it by editing the event.";

        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var mine = await Api.GetAsync<List<EventItem>>("api/events/mine");
        var failure = HandleCommonFailure(mine);
        if (failure != null) return failure;

        // only events this organiser owns show up in the list, so anything else is "not found"
        var ev = mine.Data?.FirstOrDefault(e => e.EventId == id);
        if (ev == null) return NotFound();

        return View("EventForm", new EventFormViewModel
        {
            EventId = ev.EventId,
            Name = ev.Name,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            Distance = ev.Distance,
            EventType = ev.EventType,
            BannerUrl = ev.BannerUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(int id, EventFormViewModel model)
    {
        model.EventId = id;

        var bannerError = CheckBanner(model.BannerFile);
        if (bannerError != null)
            ModelState.AddModelError(nameof(model.BannerFile), bannerError);

        if (!ModelState.IsValid)
            return View("EventForm", model);

        var result = await Api.PutAsync<EventItem>($"api/events/{id}", ToApiEvent(model));
        var failure = HandleCommonFailure(result);
        if (failure != null) return failure;

        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "We couldn't save your changes. Please try again.");
            return View("EventForm", model);
        }

        var warning = await UploadBannerAsync(id, model.BannerFile);
        if (warning == null)
            TempData["Success"] = "Your changes have been saved.";
        else
            TempData["Error"] = $"Your changes were saved, but the banner didn't upload: {warning}";

        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await Api.DeleteAsync($"api/events/{id}");
        var failure = HandleCommonFailure(result);
        if (failure != null) return failure;

        // a 409 lands here too, with the API's own explanation (the event already has enrolments)
        if (result.Ok)
            TempData["Success"] = "The event was deleted.";
        else
            TempData["Error"] = result.Message ?? "We couldn't delete that event.";

        return RedirectToAction(nameof(Events));
    }

    // ---------- categories ----------

    public async Task<IActionResult> Categories(int eventId) =>
        await ShowCategoriesAsync(eventId, new CategoryFormViewModel { EventId = eventId });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
            return await ShowCategoriesAsync(model.EventId, model);

        var result = await Api.PostAsync<CategoryItem>($"api/events/{model.EventId}/categories", ToApiCategory(model));
        var failure = HandleCommonFailure(result);
        if (failure != null) return failure;

        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "We couldn't add that category. Please try again.");
            return await ShowCategoriesAsync(model.EventId, model);
        }

        TempData["Success"] = "Category added.";
        return RedirectToAction(nameof(Categories), new { eventId = model.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id, int eventId)
    {
        var result = await Api.GetAsync<List<CategoryItem>>($"api/events/{eventId}/categories");
        var failure = HandleCommonFailure(result);
        if (failure != null) return failure;

        var category = result.Data?.FirstOrDefault(c => c.CategoryId == id);
        if (category == null) return NotFound();

        return View(new CategoryFormViewModel
        {
            CategoryId = category.CategoryId,
            EventId = category.EventId,
            Name = category.Name,
            Distance = category.Distance,
            Price = category.Price
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await Api.PutAsync<CategoryItem>($"api/categories/{model.CategoryId}", ToApiCategory(model));
        var failure = HandleCommonFailure(result);
        if (failure != null) return failure;

        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "We couldn't save that category. Please try again.");
            return View(model);
        }

        TempData["Success"] = "Category updated.";
        return RedirectToAction(nameof(Categories), new { eventId = model.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        var result = await Api.DeleteAsync($"api/categories/{id}");
        var failure = HandleCommonFailure(result);
        if (failure != null) return failure;

        if (result.Ok)
            TempData["Success"] = "The category was deleted.";
        else
            TempData["Error"] = result.Message ?? "We couldn't delete that category.";

        return RedirectToAction(nameof(Categories), new { eventId });
    }

    // ---------- enrolments and results ----------

    public async Task<IActionResult> Enrolments(int? eventId) => await ShowEventPageAsync("Enrolments", eventId);

    public async Task<IActionResult> Results(int? eventId) => await ShowEventPageAsync("Results", eventId);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CaptureResult(CaptureResultViewModel model)
    {
        var back = RedirectToAction(nameof(Results), new { eventId = model.EventId });

        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? "Please check the finish time and position.";
            return back;
        }

        var time = model.FinishTime.Trim();
        if (!TimePattern.IsMatch(time))
        {
            TempData["Error"] = "Enter the finish time as hh:mm:ss, for example 01:45:30.";
            return back;
        }

        var finishTime = TimeSpan.Parse(time, CultureInfo.InvariantCulture);

        var result = await Api.PostAsync<ResultItem>("api/results", new
        {
            enrolmentId = model.EnrolmentId,
            finishTime = finishTime.ToString("c", CultureInfo.InvariantCulture),
            position = model.Position
        });

        var failure = HandleCommonFailure(result);
        if (failure != null) return failure;

        if (result.Ok)
            TempData["Success"] = "Result saved. The participant can see it now.";
        else
            TempData["Error"] = result.Message ?? "We couldn't save that result.";

        return back;
    }

    // ---------- profile ----------

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var result = await Api.GetAsync<ProfileItem>("api/organisers/me");
        var failure = HandleCommonFailure(result);
        if (failure != null) return failure;

        return View(new ProfileFormViewModel
        {
            Name = result.Data?.Name ?? string.Empty,
            Email = result.Data?.Email ?? string.Empty
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await Api.PutAsync<ProfileItem>("api/organisers/me", new
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

    // ---------- helpers ----------

    private async Task<IActionResult> ShowCategoriesAsync(int eventId, CategoryFormViewModel form)
    {
        var mine = await Api.GetAsync<List<EventItem>>("api/events/mine");
        var failure = HandleCommonFailure(mine);
        if (failure != null) return failure;

        var ev = mine.Data?.FirstOrDefault(e => e.EventId == eventId);
        if (ev == null) return NotFound();

        var categories = await Api.GetAsync<List<CategoryItem>>($"api/events/{eventId}/categories");
        failure = HandleCommonFailure(categories);
        if (failure != null) return failure;

        return View("Categories", new CategoriesPageViewModel
        {
            Event = ev,
            Categories = categories.Data ?? new List<CategoryItem>(),
            NewCategory = form
        });
    }

    private async Task<IActionResult> ShowEventPageAsync(string view, int? eventId)
    {
        var mine = await Api.GetAsync<List<EventItem>>("api/events/mine");
        var failure = HandleCommonFailure(mine);
        if (failure != null) return failure;

        var events = mine.Data ?? new List<EventItem>();
        var model = new EventEnrolmentsViewModel { Events = events };

        if (events.Count == 0)
            return View(view, model);

        // no event chosen yet: start with one that has people in it
        var selected = eventId ?? (events.FirstOrDefault(e => (e.TotalEnrolments ?? 0) > 0) ?? events[0]).EventId;
        if (events.All(e => e.EventId != selected))
            return NotFound();

        var enrolments = await Api.GetAsync<List<EnrolmentItem>>($"api/events/{selected}/enrolments");
        failure = HandleCommonFailure(enrolments);
        if (failure != null) return failure;

        model.SelectedEventId = selected;
        model.Enrolments = enrolments.Data ?? new List<EnrolmentItem>();
        return View(view, model);
    }

    // a quick check so people get a friendly message before the upload even starts. the API checks again
    private static string? CheckBanner(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;
        if (file.Length > MaxImageBytes) return "That image is too big. The limit is 5 MB.";
        if (!ImageTypes.Contains(file.ContentType.ToLowerInvariant())) return "Only JPG, PNG or WEBP images are allowed.";
        return null;
    }

    // returns a message if the banner didn't make it, or null when all went well (or there was no banner)
    private async Task<string?> UploadBannerAsync(int eventId, IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;

        var upload = await Api.UploadAsync<ImageUploadItem>($"api/events/{eventId}/banner", file);
        return upload.Ok ? null : upload.Message ?? "The banner couldn't be uploaded.";
    }

    private static object ToApiEvent(EventFormViewModel m) => new
    {
        name = m.Name.Trim(),
        description = string.IsNullOrWhiteSpace(m.Description) ? null : m.Description.Trim(),
        eventDate = m.EventDate!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        location = m.Location.Trim(),
        distance = m.Distance!.Value,
        eventType = m.EventType
    };

    private static object ToApiCategory(CategoryFormViewModel m) => new
    {
        name = m.Name.Trim(),
        distance = m.Distance!.Value,
        price = m.Price!.Value
    };
}