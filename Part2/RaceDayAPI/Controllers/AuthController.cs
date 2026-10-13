using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Filters;
using RaceDayAPI.Helpers;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayContext _context;
    private readonly IConfiguration _config;

    public AuthController(RaceDayContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    /// <summary>
    /// Registers a new Organiser or Participant account.
    /// </summary>
    /// <remarks>
    /// Send name, email, password (min 8 chars) and role ("Organiser" or "Participant").
    /// Registering as an Organiser also needs a valid organiserAccessCode.
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var isOrganiser = string.Equals(request.Role, Roles.Organiser, StringComparison.OrdinalIgnoreCase);
        var isParticipant = string.Equals(request.Role, Roles.Participant, StringComparison.OrdinalIgnoreCase);

        if (!isOrganiser && !isParticipant)
            return BadRequest(new { message = "Role must be either Organiser or Participant." });

        // organisers can't just sign themselves up, they need the secret code
        if (isOrganiser)
        {
            var expectedCode = _config["OrganiserAccessCode"];
            if (string.IsNullOrEmpty(expectedCode) || request.OrganiserAccessCode != expectedCode)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "A valid organiser access code is required to register as an Organiser." });
        }

        var email = request.Email.Trim().ToLower();

        // one email = one account across BOTH tables, so nobody ends up with two roles
        var emailTaken = await _context.Organisers.AnyAsync(o => o.Email == email)
                      || await _context.Participants.AnyAsync(p => p.Email == email);
        if (emailTaken)
            return Conflict(new { message = "That email is already registered." });

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        AuthResponse response;

        if (isOrganiser)
        {
            var organiser = new Organiser
            {
                Name = request.Name.Trim(),
                Email = email,
                PasswordHash = hash,
                CreatedAt = DateTime.Now
            };
            _context.Organisers.Add(organiser);
            await _context.SaveChangesAsync();

            response = new AuthResponse
            {
                UserId = organiser.OrganiserId,
                Name = organiser.Name,
                Email = organiser.Email,
                Role = Roles.Organiser
            };
        }
        else
        {
            var participant = new Participant
            {
                Name = request.Name.Trim(),
                Email = email,
                PasswordHash = hash,
                CreatedAt = DateTime.Now
            };
            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();

            response = new AuthResponse
            {
                UserId = participant.ParticipantId,
                Name = participant.Name,
                Email = participant.Email,
                Role = Roles.Participant
            };
        }

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Logs in an Organiser or Participant and starts a session.
    /// </summary>
    /// <remarks>
    /// Send email and password. On success the server stores the user ID and role in the session.
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLower();

        var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.Email == email);
        if (organiser != null && BCrypt.Net.BCrypt.Verify(request.Password, organiser.PasswordHash))
        {
            StartSession(organiser.OrganiserId, Roles.Organiser);
            return Ok(new AuthResponse
            {
                UserId = organiser.OrganiserId,
                Name = organiser.Name,
                Email = organiser.Email,
                Role = Roles.Organiser
            });
        }

        var participant = await _context.Participants.FirstOrDefaultAsync(p => p.Email == email);
        if (participant != null && BCrypt.Net.BCrypt.Verify(request.Password, participant.PasswordHash))
        {
            StartSession(participant.ParticipantId, Roles.Participant);
            return Ok(new AuthResponse
            {
                UserId = participant.ParticipantId,
                Name = participant.Name,
                Email = participant.Email,
                Role = Roles.Participant
            });
        }

        // same message whether the email or the password was wrong, so we don't leak which accounts exist
        return Unauthorized(new { message = "Invalid email or password." });
    }

    /// <summary>
    /// Logs out the current user and clears their session.
    /// </summary>
    [HttpPost("logout")]
    [SessionAuthorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }

    private void StartSession(int userId, string role)
    {
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, userId);
        HttpContext.Session.SetString(SessionKeys.Role, role);
    }
}