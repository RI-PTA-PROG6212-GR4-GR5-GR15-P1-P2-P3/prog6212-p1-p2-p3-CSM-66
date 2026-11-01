using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Filters;
using RaceDayAPI.Helpers;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api")]
public class ImagesController : ControllerBase
{
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly Dictionary<string, string> AllowedTypes = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly RaceDayContext _context;
    private readonly IBlobStorageService _blobs;

    public ImagesController(RaceDayContext context, IBlobStorageService blobs)
    {
        _context = context;
        _blobs = blobs;
    }

    /// <summary>
    /// Uploads a banner image for an event and saves its URL. Only the organiser who owns the event.
    /// </summary>
    /// <remarks>
    /// Send the image as form data in a field called file. JPG, PNG or WEBP, 5 MB at most.
    /// A new banner replaces the old one.
    /// </remarks>
    [HttpPost("events/{id:int}/banner")]
    [SessionAuthorize(Roles.Organiser)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadBanner(int id, [FromForm] ImageUploadRequest request)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        if (ev.OrganiserId != organiserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only change the banner of your own events." });

        if (!_blobs.IsConfigured)
            return StorageUnavailable();

        var check = await CheckImageAsync(request.File);
        if (check.Error != null)
            return BadRequest(new { message = check.Error });

        var blobName = $"banners/event-{id}-{Guid.NewGuid():N}{check.Extension}";

        string url;
        try
        {
            url = await UploadAsync(request.File!, blobName, check.ContentType);
        }
        catch (RequestFailedException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "The image couldn't be saved. Please try again." });
        }

        var oldUrl = ev.BannerUrl;
        ev.BannerUrl = url;
        await _context.SaveChangesAsync();
        await _blobs.DeleteIfExistsAsync(oldUrl);

        return Ok(new ImageUploadResponse { Url = url });
    }

    /// <summary>
    /// Uploads a profile picture for the logged in participant and saves its URL.
    /// </summary>
    /// <remarks>
    /// Send the image as form data in a field called file. JPG, PNG or WEBP, 5 MB at most.
    /// A new picture replaces the old one.
    /// </remarks>
    [HttpPost("participants/me/picture")]
    [SessionAuthorize(Roles.Participant)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadPicture([FromForm] ImageUploadRequest request)
    {
        var participantId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        var participant = await _context.Participants.FindAsync(participantId);
        if (participant == null)
            return NotFound(new { message = "Profile not found." });

        if (!_blobs.IsConfigured)
            return StorageUnavailable();

        var check = await CheckImageAsync(request.File);
        if (check.Error != null)
            return BadRequest(new { message = check.Error });

        var blobName = $"profiles/participant-{participantId}-{Guid.NewGuid():N}{check.Extension}";

        string url;
        try
        {
            url = await UploadAsync(request.File!, blobName, check.ContentType);
        }
        catch (RequestFailedException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "The image couldn't be saved. Please try again." });
        }

        var oldUrl = participant.ProfilePictureUrl;
        participant.ProfilePictureUrl = url;
        await _context.SaveChangesAsync();
        await _blobs.DeleteIfExistsAsync(oldUrl);

        return Ok(new ImageUploadResponse { Url = url });
    }

    private IActionResult StorageUnavailable() =>
        StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image uploads aren't available right now." });

    private async Task<string> UploadAsync(IFormFile file, string blobName, string contentType)
    {
        await using var stream = file.OpenReadStream();
        return await _blobs.UploadImageAsync(stream, blobName, contentType);
    }

    // checks size, type and the real file header, so a renamed .txt can't sneak through
    private static async Task<(string? Error, string ContentType, string Extension)> CheckImageAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return ("Please choose an image to upload.", "", "");

        if (file.Length > MaxBytes)
            return ("That image is too big. The limit is 5 MB.", "", "");

        var contentType = file.ContentType.ToLowerInvariant();
        if (!AllowedTypes.TryGetValue(contentType, out var extension))
            return ("Only JPG, PNG or WEBP images are allowed.", "", "");

        if (!await MatchesSignatureAsync(file, contentType))
            return ("That file doesn't look like a real image.", "", "");

        return (null, contentType, extension);
    }

    private static async Task<bool> MatchesSignatureAsync(IFormFile file, string contentType)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header, 0, header.Length);
        if (read < 12)
            return false;

        return contentType switch
        {
            "image/jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
            "image/webp" => header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F'
                            && header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P',
            _ => false
        };
    }
}