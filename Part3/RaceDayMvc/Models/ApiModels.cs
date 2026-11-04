namespace RaceDayMvc.Models;

// these mirror what the API sends back

public class EventItem
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? BannerUrl { get; set; }
    public int? TotalEnrolments { get; set; }
    public List<CategoryItem>? Categories { get; set; }
}

public class CategoryItem
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public decimal Price { get; set; }
}

public class EnrolmentItem
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool ResultCaptured { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
}

public class ResultItem
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public int TotalFinishers { get; set; }
}

public class ProfileItem
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ProfilePictureUrl { get; set; }
}