namespace RaceDayMvc.Helpers;

public static class NumberExtensions
{
    // 1 -> 1st, 2 -> 2nd, 3 -> 3rd, 4 -> 4th, 11 -> 11th, 21 -> 21st
    public static string ToOrdinal(this int number)
    {
        var lastTwo = number % 100;
        if (lastTwo is >= 11 and <= 13)
            return $"{number}th";

        return (number % 10) switch
        {
            1 => $"{number}st",
            2 => $"{number}nd",
            3 => $"{number}rd",
            _ => $"{number}th"
        };
    }
}