using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayAPI.Models;

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(6,2)")]
    public decimal Distance { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal Price { get; set; }

    public Event? Event { get; set; }
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}