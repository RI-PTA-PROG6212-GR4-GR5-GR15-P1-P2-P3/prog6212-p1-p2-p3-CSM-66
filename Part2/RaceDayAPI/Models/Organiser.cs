using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.Models;

public class Organiser
{
    public int OrganiserId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    // bcrypt hash only, we never store the real password
    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<Result> CapturedResults { get; set; } = new List<Result>();
}