namespace RaceDayMvc.Helpers;

public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string UserName = "UserName";
    public const string Role = "Role";

    // the API's own session cookie, kept here per user so every call to the API is made as that user
    public const string ApiCookie = "ApiCookie";
}

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public static class SessionUserExtensions
{
    public static int? GetUserId(this ISession session) => session.GetInt32(SessionKeys.UserId);
    public static string? GetUserName(this ISession session) => session.GetString(SessionKeys.UserName);
    public static string? GetRole(this ISession session) => session.GetString(SessionKeys.Role);

    public static bool IsLoggedIn(this ISession session) =>
        session.GetInt32(SessionKeys.UserId) != null && session.GetString(SessionKeys.Role) != null;
}