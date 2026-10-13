using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayAPI.Models;

public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "date")]
    public DateTime EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Column(TypeName = "decimal(6,2)")]
    public decimal Distance { get; set; }

    // Run, Walk or Cycle
    [Required, MaxLength(20)]
    public string EventType { get; set; } = "Run";

    public DateTime CreatedAt { get; set; }

    public Organiser? Organiser { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}