using System.Net.Http.Headers;

namespace ChatRoom.Services;

public class JwtAuthorizationHandler(ChatAuthStateProvider authStateProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (authStateProvider.IsAuthenticated && authStateProvider.AccessToken is { } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            authStateProvider.SignOut();
        }

        return response;
    }
}
