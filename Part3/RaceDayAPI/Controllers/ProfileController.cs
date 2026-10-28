using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Filters;
using RaceDayAPI.Helpers;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api")]
public class ProfileController : ControllerBase
{
    private readonly RaceDayContext _context;

    public ProfileController(RaceDayContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns the logged in participant's own profile.
    /// </summary>
    [HttpGet("participants/me")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetParticipant()
    {
        var id = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        var p = await _context.Participants.FindAsync(id);
        if (p == null)
            return NotFound(new { message = "Profile not found." });

        return Ok(new ProfileResponse
        {
            UserId = p.ParticipantId,
            Name = p.Name,
            Email = p.Email,
            Role = Roles.Participant,
            CreatedAt = p.CreatedAt
        });
    }

    /// <summary>
    /// Updates the logged in participant's name and email.
    /// </summary>
    /// <remarks>
    /// The email can't already belong to any other account (Organiser or Participant).
    /// </remarks>
    [HttpPut("participants/me")]
    [SessionAuthorize(Roles.Participant)]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateParticipant(ProfileRequest request)
    {
        var id = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        var p = await _context.Participants.FindAsync(id);
        if (p == null)
            return NotFound(new { message = "Profile not found." });

        var email = request.Email.Trim().ToLower();

        var taken = await _context.Organisers.AnyAsync(o => o.Email == email)
                 || await _context.Participants.AnyAsync(x => x.Email == email && x.ParticipantId != id);
        if (taken)
            return Conflict(new { message = "That email is already registered." });

        p.Name = request.Name.Trim();
        p.Email = email;
        await _context.SaveChangesAsync();

        return Ok(new ProfileResponse
        {
            UserId = p.ParticipantId,
            Name = p.Name,
            Email = p.Email,
            Role = Roles.Participant,
            CreatedAt = p.CreatedAt
        });
    }

    /// <summary>
    /// Returns the logged in organiser's own profile.
    /// </summary>
    [HttpGet("organisers/me")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganiser()
    {
        var id = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        var o = await _context.Organisers.FindAsync(id);
        if (o == null)
            return NotFound(new { message = "Profile not found." });

        return Ok(new ProfileResponse
        {
            UserId = o.OrganiserId,
            Name = o.Name,
            Email = o.Email,
            Role = Roles.Organiser,
            CreatedAt = o.CreatedAt
        });
    }

    /// <summary>
    /// Updates the logged in organiser's name and email.
    /// </summary>
    /// <remarks>
    /// The email can't already belong to any other account (Organiser or Participant).
    /// </remarks>
    [HttpPut("organisers/me")]
    [SessionAuthorize(Roles.Organiser)]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateOrganiser(ProfileRequest request)
    {
        var id = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        var o = await _context.Organisers.FindAsync(id);
        if (o == null)
            return NotFound(new { message = "Profile not found." });

        var email = request.Email.Trim().ToLower();

        var taken = await _context.Participants.AnyAsync(p => p.Email == email)
                 || await _context.Organisers.AnyAsync(x => x.Email == email && x.OrganiserId != id);
        if (taken)
            return Conflict(new { message = "That email is already registered." });

        o.Name = request.Name.Trim();
        o.Email = email;
        await _context.SaveChangesAsync();

        return Ok(new ProfileResponse
        {
            UserId = o.OrganiserId,
            Name = o.Name,
            Email = o.Email,
            Role = Roles.Organiser,
            CreatedAt = o.CreatedAt
        });
    }
}