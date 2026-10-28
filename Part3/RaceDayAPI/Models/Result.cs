namespace RaceDayAPI.Models;

public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }

    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }

    public int CapturedByOrganiserId { get; set; }

    public Enrolment? Enrolment { get; set; }
    public Organiser? CapturedByOrganiser { get; set; }
}