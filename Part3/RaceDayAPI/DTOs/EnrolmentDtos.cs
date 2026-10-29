using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.DTOs;

public class EnrolmentRequest
{
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

public class EnrolmentResponse
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

    // so the results page knows who still needs a result
    public bool ResultCaptured { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
}

public class ResultRequest
{
    [Range(1, int.MaxValue)]
    public int EnrolmentId { get; set; }

    // format hh:mm:ss, e.g. "00:52:30"
    [Required]
    public TimeSpan? FinishTime { get; set; }

    [Range(1, 100000)]
    public int Position { get; set; }
}

public class ResultResponse
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }

    // how many people have a result in the same category ("4th out of 12")
    public int TotalFinishers { get; set; }

    public int CapturedByOrganiserId { get; set; }
}