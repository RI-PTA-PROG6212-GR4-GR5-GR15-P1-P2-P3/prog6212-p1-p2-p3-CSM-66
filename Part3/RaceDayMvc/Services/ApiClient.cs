using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RaceDayMvc.Helpers;
using RaceDayMvc.Models;

namespace RaceDayMvc.Services;

// the one place this app talks to the API. no database, no Azure, just HTTP calls
public class ApiClient
{
    // the name of the session cookie the API hands out
    private const string ApiCookieName = ".AspNetCore.Session";

    private const string UnreachableMessage = "We can't reach the RaceDay service right now. Please try again in a moment.";
    private const string ServerProblemMessage = "Something went wrong on our side. Please try again.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public ApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    private ISession? Session => _accessor.HttpContext?.Session;

    // ---------- the calls the controllers use ----------

    public Task<ApiResult<T>> GetAsync<T>(string url) =>
        SendAsync<T>(HttpMethod.Get, url, null);

    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) =>
        SendAsync<T>(HttpMethod.Post, url, ToJson(body));

    public Task<ApiResult<T>> PutAsync<T>(string url, object body) =>
        SendAsync<T>(HttpMethod.Put, url, ToJson(body));

    public Task<ApiResult> PostAsync(string url, object? body = null) =>
        SendAsync(HttpMethod.Post, url, ToJson(body));

    public Task<ApiResult> DeleteAsync(string url) =>
        SendAsync(HttpMethod.Delete, url, null);

    // sends an image to the API. the API is the one that talks to Azure Blob Storage
    public Task<ApiResult<T>> UploadAsync<T>(string url, IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());

        if (MediaTypeHeaderValue.TryParse(file.ContentType, out var mediaType))
            fileContent.Headers.ContentType = mediaType;

        content.Add(fileContent, "file", file.FileName);
        return SendAsync<T>(HttpMethod.Post, url, content);
    }

    public Task<ApiResult<AuthUser>> LoginAsync(string email, string password) =>
        PostAsync<AuthUser>("api/auth/login", new { email, password });

    // ---------- the plumbing ----------

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content)
    {
        var raw = await SendRawAsync(method, url, content);

        if (!raw.Ok)
            return new ApiResult<T> { Ok = false, Status = raw.Status, Message = raw.Message };

        // a success with no body (like 204 No Content) is fine
        if (string.IsNullOrWhiteSpace(raw.Body))
            return new ApiResult<T> { Ok = true, Status = raw.Status };

        try
        {
            var data = JsonSerializer.Deserialize<T>(raw.Body, JsonOptions);
            return new ApiResult<T> { Ok = true, Status = raw.Status, Data = data };
        }
        catch (JsonException)
        {
            return new ApiResult<T> { Ok = false, Status = 500, Message = ServerProblemMessage };
        }
    }

    private async Task<ApiResult> SendAsync(HttpMethod method, string url, HttpContent? content)
    {
        var raw = await SendRawAsync(method, url, content);
        return new ApiResult { Ok = raw.Ok, Status = raw.Status, Message = raw.Message };
    }

    private async Task<RawResponse> SendRawAsync(HttpMethod method, string url, HttpContent? content)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };

        // make the call as this user by passing along their API session
        var apiCookie = Session?.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrEmpty(apiCookie))
            request.Headers.TryAddWithoutValidation("Cookie", $"{ApiCookieName}={apiCookie}");

        try
        {
            using var response = await _http.SendAsync(request);
            CaptureApiCookie(response);

            var body = await response.Content.ReadAsStringAsync();
            var status = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
                return new RawResponse(true, status, body, null);

            return new RawResponse(false, status, body, ReadError(body, status));
        }
        catch (HttpRequestException)
        {
            return new RawResponse(false, 0, string.Empty, UnreachableMessage);
        }
        catch (TaskCanceledException)
        {
            return new RawResponse(false, 0, string.Empty, UnreachableMessage);
        }
    }

    // when the API starts a session (login) it sends a cookie. we keep it in this user's own session
    private void CaptureApiCookie(HttpResponseMessage response)
    {
        var session = Session;
        if (session == null || !response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return;

        foreach (var cookie in cookies)
        {
            if (!cookie.StartsWith(ApiCookieName + "=", StringComparison.Ordinal))
                continue;

            var value = cookie[(ApiCookieName.Length + 1)..].Split(';')[0];
            session.SetString(SessionKeys.ApiCookie, value);
        }
    }

    private static StringContent? ToJson(object? body) =>
        body == null
            ? null
            : new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

    // turns whatever the API sent back into one friendly sentence
    private static string ReadError(string body, int status)
    {
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                        return message.GetString()!;

                    // validation problems come back as { errors: { Field: ["..."] } }
                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        var messages = errors.EnumerateObject()
                            .Where(p => p.Value.ValueKind == JsonValueKind.Array)
                            .SelectMany(p => p.Value.EnumerateArray().Select(m => m.GetString()))
                            .Where(m => !string.IsNullOrWhiteSpace(m))
                            .ToList();

                        if (messages.Count > 0)
                            return string.Join(" ", messages);
                    }
                }
            }
            catch (JsonException)
            {
                // not JSON, fall through to the generic messages
            }
        }

        return status switch
        {
            401 => "Please log in to continue.",
            403 => "You don't have access to that.",
            404 => "We couldn't find that.",
            >= 500 => ServerProblemMessage,
            _ => "That didn't work. Please check what you entered and try again."
        };
    }

    private sealed record RawResponse(bool Ok, int Status, string Body, string? Message);
}