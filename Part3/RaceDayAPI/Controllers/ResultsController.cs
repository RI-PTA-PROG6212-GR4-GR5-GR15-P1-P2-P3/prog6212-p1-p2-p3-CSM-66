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
    /// Capturing a result marks the enrolment as Completed.
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

        // once there's a result the enrolment is done
        enrolment.Status = EnrolmentStatus.Completed;

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        result.Enrolment = enrolment;
        var total = await _context.Results.CountAsync(r => r.Enrolment!.CategoryId == enrolment.CategoryId);
        return StatusCode(StatusCodes.Status201Created, MapResult(result, total));
    }

    /// <summary>
    /// Lists the logged in participant's own results across all events they entered.
    /// </summary>
    /// <remarks>
    /// Each result includes totalFinishers, the number of people with a result in the same category.
    /// </remarks>
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

        // one query for all the "out of N finishers" numbers
        var categoryIds = results.Select(r => r.Enrolment!.CategoryId).Distinct().ToList();
        var totals = await _context.Results
            .Where(r => categoryIds.Contains(r.Enrolment!.CategoryId))
            .GroupBy(r => r.Enrolment!.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        return Ok(results.Select(r => MapResult(r, totals.GetValueOrDefault(r.Enrolment!.CategoryId))));
    }

    private static ResultResponse MapResult(Result r, int totalFinishers) => new()
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        EventId = r.Enrolment?.Category?.EventId ?? 0,
        EventName = r.Enrolment?.Category?.Event?.Name ?? string.Empty,
        EventDate = r.Enrolment?.Category?.Event?.EventDate ?? default,
        CategoryName = r.Enrolment?.Category?.Name ?? string.Empty,
        FinishTime = r.FinishTime,
        Position = r.Position,
        TotalFinishers = totalFinishers,
        CapturedByOrganiserId = r.CapturedByOrganiserId
    };
}