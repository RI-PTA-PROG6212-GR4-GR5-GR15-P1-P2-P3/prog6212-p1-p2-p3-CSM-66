using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int CategoryId { get; set; }

    public DateTime EnrolmentDate { get; set; }

    // Confirmed when they enrol, Completed once their result is captured
    [Required, MaxLength(20)]
    public string Status { get; set; } = "Confirmed";

    public Participant? Participant { get; set; }
    public Category? Category { get; set; }
    public Result? Result { get; set; }
}