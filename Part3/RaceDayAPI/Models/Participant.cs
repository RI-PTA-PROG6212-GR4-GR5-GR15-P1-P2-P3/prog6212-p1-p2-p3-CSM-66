using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.Models;

public class Participant
{
    public int ParticipantId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    // link to the profile picture in Azure Blob Storage
    [MaxLength(500)]
    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}