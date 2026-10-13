using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Filters;
using RaceDayAPI.Helpers;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/results")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayContext _context;

    public ResultsController(RaceDayContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Captures a finish time and position for a participant's enrolment. Organisers only.
    /// </summary>
    /// <remarks>
    /// Send enrolmentId, finishTime (hh:mm:ss, e.g. "00:52:30") and position.
    /// Only the organiser who owns the event can capture its results, and each enrolment gets one result (409 if it already has one).
    /// </remarks>
    [HttpPost]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture(ResultRequest request)
    {
        var enrolment = await _context.Enrolments
            .Include(e => e.Category)
            .ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);
        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        if (enrolment.Category!.Event!.OrganiserId != organiserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only capture results for your own events." });

        if (await _context.Results.AnyAsync(r => r.EnrolmentId == request.EnrolmentId))
            return Conflict(new { message = "A result has already been captured for this enrolment." });

        var result = new Result
        {
            EnrolmentId = request.EnrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position,
            CapturedByOrganiserId = organiserId
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        result.Enrolment = enrolment;
        return StatusCode(StatusCodes.Status201Created, MapResult(result));
    }

    /// <summary>
    /// Lists the logged in participant's own results across all events they entered.
    /// </summary>
    [HttpGet("me")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var participantId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var results = await _context.Results
            .Include(r => r.Enrolment)
            .ThenInclude(e => e!.Category)
            .ThenInclude(c => c!.Event)
            .Where(r => r.Enrolment!.ParticipantId == participantId)
            .OrderByDescending(r => r.Enrolment!.Category!.Event!.EventDate)
            .ToListAsync();

        return Ok(results.Select(MapResult));
    }

    private static ResultResponse MapResult(Result r) => new()
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        EventId = r.Enrolment?.Category?.EventId ?? 0,
        EventName = r.Enrolment?.Category?.Event?.Name ?? string.Empty,
        CategoryName = r.Enrolment?.Category?.Name ?? string.Empty,
        FinishTime = r.FinishTime,
        Position = r.Position,
        CapturedByOrganiserId = r.CapturedByOrganiserId
    };
}