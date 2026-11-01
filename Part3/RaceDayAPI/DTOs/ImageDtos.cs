using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.DTOs;

public class ImageUploadRequest
{
    [Required]
    public IFormFile? File { get; set; }
}

public class ImageUploadResponse
{
    public string Url { get; set; } = string.Empty;
}