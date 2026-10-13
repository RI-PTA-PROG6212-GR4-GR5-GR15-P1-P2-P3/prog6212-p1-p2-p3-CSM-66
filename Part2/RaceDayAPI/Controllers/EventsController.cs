using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Filters;
using RaceDayAPI.Helpers;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayContext _context;

    public EventsController(RaceDayContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lists all upcoming events. Public, no login needed.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var events = await _context.Events
            .Include(e => e.Organiser)
            .Where(e => e.EventDate >= DateTime.Today)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events.Select(e => MapEvent(e, false)));
    }

    /// <summary>
    /// Returns one event with all of its categories. Public, no login needed.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _context.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null)
            return NotFound(new { message = "Event not found." });

        return Ok(MapEvent(ev, true));
    }

    /// <summary>
    /// Creates a new event. Organisers only.
    /// </summary>
    /// <remarks>
    /// Send name, description, eventDate, location, distance and eventType (Run, Walk or Cycle).
    /// The event is linked to the logged in organiser automatically.
    /// </remarks>
    [HttpPost]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventRequest request)
    {
        var type = NormaliseType(request.EventType);
        if (type == null)
            return BadRequest(new { message = "Event type must be Run, Walk or Cycle." });

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var ev = new Event
        {
            OrganiserId = organiserId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            EventDate = request.EventDate!.Value.Date,
            Location = request.Location.Trim(),
            Distance = request.Distance,
            EventType = type,
            CreatedAt = DateTime.Now
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        await _context.Entry(ev).Reference(e => e.Organiser).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, MapEvent(ev, false));
    }

    /// <summary>
    /// Updates an event. Only the organiser who created it can do this.
    /// </summary>
    [HttpPut("{id}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventRequest request)
    {
        var type = NormaliseType(request.EventType);
        if (type == null)
            return BadRequest(new { message = "Event type must be Run, Walk or Cycle." });

        var ev = await _context.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        if (ev.OrganiserId != organiserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only edit your own events." });

        ev.Name = request.Name.Trim();
        ev.Description = request.Description?.Trim();
        ev.EventDate = request.EventDate!.Value.Date;
        ev.Location = request.Location.Trim();
        ev.Distance = request.Distance;
        ev.EventType = type;

        await _context.SaveChangesAsync();
        return Ok(MapEvent(ev, false));
    }

    /// <summary>
    /// Deletes an event and its categories. Only the organiser who created it can do this.
    /// </summary>
    /// <remarks>
    /// If anyone has already enrolled in one of its categories the delete is refused (409).
    /// </remarks>
    [HttpDelete("{id}")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _context.Events
            .Include(e => e.Categories)
            .ThenInclude(c => c.Enrolments)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null)
            return NotFound(new { message = "Event not found." });

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        if (ev.OrganiserId != organiserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only delete your own events." });

        if (ev.Categories.Any(c => c.Enrolments.Any()))
            return Conflict(new { message = "This event already has enrolments, so it can't be deleted." });

        _context.Categories.RemoveRange(ev.Categories);
        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static string? NormaliseType(string type) => type.Trim().ToLower() switch
    {
        "run" => "Run",
        "walk" => "Walk",
        "cycle" => "Cycle",
        _ => null
    };

    private static EventResponse MapEvent(Event e, bool includeCategories) => new()
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser?.Name ?? string.Empty,
        Name = e.Name,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        Distance = e.Distance,
        EventType = e.EventType,
        Categories = includeCategories
            ? e.Categories.Select(c => new CategoryResponse
            {
                CategoryId = c.CategoryId,
                EventId = c.EventId,
                Name = c.Name,
                Distance = c.Distance,
                Price = c.Price
            }).ToList()
            : null
    };
}