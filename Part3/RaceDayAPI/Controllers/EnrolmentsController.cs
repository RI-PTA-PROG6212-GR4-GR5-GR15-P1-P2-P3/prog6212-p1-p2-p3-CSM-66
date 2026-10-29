using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Filters;
using RaceDayAPI.Helpers;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayContext _context;

    public EnrolmentsController(RaceDayContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Enters the logged in participant into an event by picking one of its categories.
    /// </summary>
    /// <remarks>
    /// Send the categoryId. The system records the participant, the category and (through it) the event.
    /// You can't enrol in the same category twice (409).
    /// </remarks>
    [HttpPost("enrolments")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(EnrolmentRequest request)
    {
        var participantId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var participant = await _context.Participants.FindAsync(participantId);
        if (participant == null)
            return Unauthorized(new { message = "Please log in again." });

        var category = await _context.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId);
        if (category == null)
            return NotFound(new { message = "Category not found." });

        var already = await _context.Enrolments
            .AnyAsync(e => e.ParticipantId == participantId && e.CategoryId == request.CategoryId);
        if (already)
            return Conflict(new { message = "You are already enrolled in this category." });

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            CategoryId = request.CategoryId,
            EnrolmentDate = DateTime.Now,
            Status = EnrolmentStatus.Confirmed
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        enrolment.Participant = participant;
        enrolment.Category = category;
        return StatusCode(StatusCodes.Status201Created, MapEnrolment(enrolment));
    }

    /// <summary>
    /// Lists all of the logged in participant's own enrolments.
    /// </summary>
    [HttpGet("enrolments/me")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var participantId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var enrolments = await _context.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Result)
            .Include(e => e.Category)
            .ThenInclude(c => c!.Event)
            .Where(e => e.ParticipantId == participantId)
            .OrderBy(e => e.Category!.Event!.EventDate)
            .ToListAsync();

        return Ok(enrolments.Select(MapEnrolment));
    }

    /// <summary>
    /// Lists every enrolment for one event. Only the organiser who owns the event.
    /// </summary>
    [HttpGet("events/{eventId:int}/enrolments")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        if (ev.OrganiserId != organiserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only view enrolments for your own events." });

        var enrolments = await _context.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Result)
            .Include(e => e.Category)
            .ThenInclude(c => c!.Event)
            .Where(e => e.Category!.EventId == eventId)
            .OrderBy(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments.Select(MapEnrolment));
    }

    private static EnrolmentResponse MapEnrolment(Enrolment e) => new()
    {
        EnrolmentId = e.EnrolmentId,
        ParticipantId = e.ParticipantId,
        ParticipantName = e.Participant?.Name ?? string.Empty,
        CategoryId = e.CategoryId,
        CategoryName = e.Category?.Name ?? string.Empty,
        EventId = e.Category?.EventId ?? 0,
        EventName = e.Category?.Event?.Name ?? string.Empty,
        EventDate = e.Category?.Event?.EventDate ?? default,
        EnrolmentDate = e.EnrolmentDate,
        Status = e.Status,
        ResultCaptured = e.Result != null,
        FinishTime = e.Result?.FinishTime,
        Position = e.Result?.Position
    };
}