namespace RaceDayMvc.Services;

// what every API call hands back, so controllers never deal with raw HTTP
public class ApiResult
{
    public bool Ok { get; init; }
    public int Status { get; init; }
    public string? Message { get; init; }

    public bool IsUnauthorized => Status == 401;
    public bool IsForbidden => Status == 403;
    public bool IsNotFound => Status == 404;

    // status 0 means we couldn't reach the API at all
    public bool IsServerProblem => Status == 0 || Status >= 500;
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }
}