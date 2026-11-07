using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Filters;
using RaceDayMvc.Helpers;
using RaceDayMvc.Models;
using RaceDayMvc.Services;

namespace RaceDayMvc.Controllers;

public class EventsController : BaseController
{
    public EventsController(ApiClient api) : base(api) { }

    // anyone can look at an event, only participants see the enter form
    public async Task<IActionResult> Details(int id)
    {
        var result = await Api.GetAsync<EventItem>($"api/events/{id}");
        var failure = HandleCommonFailure(result);
        if (failure != null) return failure;
        if (result.Data == null) return NotFound();

        var model = new EventDetailsViewModel { Event = result.Data };

        // if a participant is looking, work out which categories they've already entered
        if (HttpContext.Session.GetRole() == Roles.Participant)
        {
            var mine = await Api.GetAsync<List<EnrolmentItem>>("api/enrolments/me");
            if (mine.Ok && mine.Data != null)
                model.EnrolledCategoryIds = mine.Data.Select(e => e.CategoryId).ToHashSet();
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RoleAuthorize(Roles.Participant)]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var result = await Api.PostAsync<EnrolmentItem>("api/enrolments", new { categoryId });
        var failure = HandleCommonFailure(result);
        if (failure != null) return failure;

        if (!result.Ok)
        {
            // for example "you're already enrolled in this category"
            TempData["Error"] = result.Message ?? "We couldn't enter you for that event. Please try again.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        TempData["Success"] = "You're in! Your enrolment is confirmed.";
        return RedirectToAction("Enrolments", "Participant");
    }
}