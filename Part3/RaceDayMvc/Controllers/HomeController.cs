using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Models;
using RaceDayMvc.Services;

namespace RaceDayMvc.Controllers;

public class HomeController : BaseController
{
    public HomeController(ApiClient api) : base(api) { }

    // upcoming events with search and filters. the filtering itself happens in the API
    public async Task<IActionResult> Index(string? search, string? type, string? location)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (!string.IsNullOrWhiteSpace(type)) query.Add($"type={Uri.EscapeDataString(type.Trim())}");
        if (!string.IsNullOrWhiteSpace(location)) query.Add($"location={Uri.EscapeDataString(location.Trim())}");

        var url = "api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var result = await Api.GetAsync<List<EventItem>>(url);

        if (result.IsServerProblem)
            return View("ApiError", result.Message);

        return View(new HomeViewModel
        {
            Search = search,
            Type = type,
            Location = location,
            Events = result.Data ?? new List<EventItem>(),
            Problem = result.Ok ? null : result.Message
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();

    // 404 and friends land here, so people never see a bare browser error page
    public IActionResult Status(int id) => id == 404 ? View("NotFound") : View("Error");
}