using Microsoft.EntityFrameworkCore;
using Thread.API.Data;
using Thread.API.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? throw new InvalidOperationException("Configure Cors:AllowedOrigins.");
if (builder.Configuration["Cors:AllowedOrigins:0"] is { } frontendOrigin)
{
    allowedOrigins = [frontendOrigin];
}

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Configure ConnectionStrings:DefaultConnection using user secrets or an environment variable.");

builder.Services.AddDbContext<ThreadContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddHttpClient<IReplyApiClient, ReplyApiClient>(client =>
{
    var baseAddress = builder.Configuration["ReplyApi:BaseAddress"]
        ?? throw new InvalidOperationException("Configure ReplyApi:BaseAddress.");
    client.BaseAddress = new Uri(baseAddress);
});
builder.Services.AddScoped<IThreadService, ThreadService>();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
