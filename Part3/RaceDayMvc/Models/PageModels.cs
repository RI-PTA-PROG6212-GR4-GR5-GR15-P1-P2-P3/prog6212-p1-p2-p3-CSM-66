namespace RaceDayMvc.Models;

public class HomeViewModel
{
    public string? Search { get; set; }
    public string? Type { get; set; }
    public string? Location { get; set; }
    public List<EventItem> Events { get; set; } = new();
    public string? Problem { get; set; }
}