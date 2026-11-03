using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using ChatRoom.Models;

namespace ChatRoom.Services;

public class AuthApiClient(HttpClient httpClient)
{
    public Task<AuthResponse> LoginAsync(LoginRequest request) =>
        PostAsync("api/auth/login", request);

    public Task<AuthResponse> RegisterAsync(RegisterRequest request) =>
        PostAsync("api/auth/register", request);

    public async Task<AuthResponse?> RefreshAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh");
        using var response = await SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var message = await ReadErrorMessageAsync(response);
            throw new HttpRequestException(message, null, response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new JsonException("Users.API returned an empty authentication response.");
    }

    public async Task LogoutAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
        using var response = await SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var message = await ReadErrorMessageAsync(response);
            throw new HttpRequestException(message, null, response.StatusCode);
        }
    }

    private async Task<AuthResponse> PostAsync<TRequest>(string route, TRequest body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, route)
        {
            Content = JsonContent.Create(body)
        };
        using var response = await SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var message = await ReadErrorMessageAsync(response);
            throw new HttpRequestException(message, null, response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new JsonException("Users.API returned an empty authentication response.");
    }

    private Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        return httpClient.SendAsync(request);
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
