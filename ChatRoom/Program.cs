using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ChatRoom;
using ChatRoom.Services;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var threadApiAddress = builder.Configuration["ThreadApi:BaseAddress"]
    ?? throw new InvalidOperationException("Configure ThreadApi:BaseAddress in wwwroot/appsettings.json.");
var usersApiAddress = builder.Configuration["UsersApi:BaseAddress"]
    ?? throw new InvalidOperationException("Configure UsersApi:BaseAddress in wwwroot/appsettings.json.");

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<ChatAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(services =>
    services.GetRequiredService<ChatAuthStateProvider>());

builder.Services.AddScoped(services => new HttpClient(
    new JwtAuthorizationHandler(services.GetRequiredService<ChatAuthStateProvider>())
    {
        InnerHandler = new HttpClientHandler()
    })
{
    BaseAddress = new Uri(threadApiAddress)
});

//builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(usersApiAddress) });
// Soluciono creando un nuevo HttpClient para el AuthApiClient, ya que el creado por defecto no coincide la base addres.
// Ver si a futuro es necesario agregar el encabezado de autorización en el AuthApiClient, ya que no es necesario para el login y registro.
builder.Services.AddScoped(services => new AuthApiClient(
    new HttpClient
    {
        BaseAddress = new Uri(usersApiAddress)
    }));
builder.Services.AddScoped<IThreadApiClient, ThreadApiClient>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
