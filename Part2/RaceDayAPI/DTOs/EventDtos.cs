using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.DTOs;

public class EventRequest
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public DateTime? EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Range(0.01, 9999)]
    public decimal Distance { get; set; }

    // Run, Walk or Cycle
    [Required]
    public string EventType { get; set; } = "Run";
}

public class EventResponse
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

    // only filled in when you fetch a single event
    public List<CategoryResponse>? Categories { get; set; }
}

public class CategoryRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 9999)]
    public decimal Distance { get; set; }

    [Range(0, 100000)]
    public decimal Price { get; set; }
}

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public decimal Price { get; set; }
}