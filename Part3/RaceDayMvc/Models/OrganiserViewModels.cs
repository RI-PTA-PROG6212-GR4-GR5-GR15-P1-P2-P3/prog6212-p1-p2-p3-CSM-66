using System.ComponentModel.DataAnnotations;

namespace RaceDayMvc.Models;

public class ImageUploadItem
{
    public string Url { get; set; } = string.Empty;
}

public class DashboardViewModel
{
    public List<EventItem> Events { get; set; } = new();

    public int TotalEvents => Events.Count;
    public int TotalEnrolments => Events.Sum(e => e.TotalEnrolments ?? 0);
    public int UpcomingCount => Events.Count(e => e.EventDate >= DateTime.Today);

    public EventItem? NextEvent => Events
        .Where(e => e.EventDate >= DateTime.Today)
        .OrderBy(e => e.EventDate)
        .FirstOrDefault();
}

public class EventFormViewModel
{
    // null means we're creating a new event
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Please give the event a name.")]
    [MaxLength(150, ErrorMessage = "The name can be 150 characters at most.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "The description can be 500 characters at most.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date.")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Event date")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location.")]
    [MaxLength(150, ErrorMessage = "The location can be 150 characters at most.")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance in km.")]
    [Range(0.01, 9999, ErrorMessage = "Enter a distance between 0.01 and 9999 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? Distance { get; set; }

    [Required(ErrorMessage = "Please choose an event type.")]
    [RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "Event type must be Run, Walk or Cycle.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    // the banner that's already saved (kept in a hidden field so it survives a validation error)
    public string? BannerUrl { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? BannerFile { get; set; }
}

public class CategoryFormViewModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please name the category, like 10km or Under 20.")]
    [MaxLength(100, ErrorMessage = "The name can be 100 characters at most.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance in km.")]
    [Range(0.01, 9999, ErrorMessage = "Enter a distance between 0.01 and 9999 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? Distance { get; set; }

    [Required(ErrorMessage = "Please enter the entry fee (0 for free).")]
    [Range(0, 100000, ErrorMessage = "Enter a price of 0 or more.")]
    [Display(Name = "Price (R)")]
    public decimal? Price { get; set; }
}

public class CategoriesPageViewModel
{
    public EventItem Event { get; set; } = new();
    public List<CategoryItem> Categories { get; set; } = new();
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

// used by both the enrolments page and the results page
public class EventEnrolmentsViewModel
{
    public List<EventItem> Events { get; set; } = new();
    public int? SelectedEventId { get; set; }
    public List<EnrolmentItem> Enrolments { get; set; } = new();

    public EventItem? SelectedEvent => Events.FirstOrDefault(e => e.EventId == SelectedEventId);
}

public class CaptureResultViewModel
{
    [Range(1, int.MaxValue)]
    public int EnrolmentId { get; set; }

    [Range(1, int.MaxValue)]
    public int EventId { get; set; }

    [Required(ErrorMessage = "Enter the finish time as hh:mm:ss.")]
    [Display(Name = "Finish time")]
    public string FinishTime { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "Enter the finishing position.")]
    public int Position { get; set; }
}

public class ProfileFormViewModel
{
    [Required(ErrorMessage = "Please enter your name.")]
    [MaxLength(100, ErrorMessage = "Your name can be 100 characters at most.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "That doesn't look like an email address.")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;
}