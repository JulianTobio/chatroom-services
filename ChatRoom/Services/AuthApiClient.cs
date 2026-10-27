using System.Net.Http.Json;
using System.Text.Json;
using ChatRoom.Models;

namespace ChatRoom.Services;

public class AuthApiClient(HttpClient httpClient)
{
    public Task<AuthResponse> LoginAsync(LoginRequest request) =>
        PostAsync("api/auth/login", request);

    public Task<AuthResponse> RegisterAsync(RegisterRequest request) =>
        PostAsync("api/auth/register", request);

    private async Task<AuthResponse> PostAsync<TRequest>(string route, TRequest request)
    {
        using var response = await httpClient.PostAsJsonAsync(route, request);
        if (!response.IsSuccessStatusCode)
        {
            var message = await ReadErrorMessageAsync(response);
            throw new HttpRequestException(message, null, response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new JsonException("Users.API returned an empty authentication response.");
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblemResponse>();
            if (problem?.Errors is not null)
            {
                var validationErrors = problem.Errors
                    .SelectMany(field => field.Value.Select(message => $"{field.Key}: {message}"));
                var message = string.Join(" ", validationErrors);
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }
            }

            if (!string.IsNullOrWhiteSpace(problem?.Detail))
            {
                return problem.Detail;
            }

            if (!string.IsNullOrWhiteSpace(problem?.Title))
            {
                return problem.Title;
            }
        }
        catch (JsonException)
        {
            // Fall back to the HTTP reason phrase when the API does not return a problem-details body.
        }

        return response.ReasonPhrase ?? "Users.API devolvió una respuesta de error.";
    }

    private sealed class ApiProblemResponse
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
