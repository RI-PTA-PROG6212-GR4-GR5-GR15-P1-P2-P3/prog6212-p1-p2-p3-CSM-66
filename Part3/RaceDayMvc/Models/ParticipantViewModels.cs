using System.ComponentModel.DataAnnotations;

namespace RaceDayMvc.Models;

public class EventDetailsViewModel
{
    public EventItem Event { get; set; } = new();

    // the categories this participant has already entered, so we can show them as taken
    public HashSet<int> EnrolledCategoryIds { get; set; } = new();
}

public class ParticipantProfileViewModel
{
    [Required(ErrorMessage = "Please enter your name.")]
    [MaxLength(100, ErrorMessage = "Your name can be 100 characters at most.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "That doesn't look like an email address.")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    // kept in a hidden field so the picture doesn't disappear if the form has a validation error
    public string? ProfilePictureUrl { get; set; }
}