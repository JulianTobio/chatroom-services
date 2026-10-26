using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ChatRoom;
using ChatRoom.Services;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var threadApiAddress = builder.Configuration["ThreadApi:BaseAddress"]
    ?? throw new InvalidOperationException("Configure ThreadApi:BaseAddress in wwwroot/appsettings.json.");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(threadApiAddress) });
builder.Services.AddScoped<IThreadApiClient, ThreadApiClient>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
