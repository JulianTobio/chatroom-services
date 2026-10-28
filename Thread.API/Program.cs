using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Thread.API.Data;
using Thread.API.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Configure Jwt:Issuer.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Configure Jwt:Audience.");
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Configure Jwt:SigningKey using an environment variable or user secrets.");
if (Encoding.UTF8.GetByteCount(jwtSigningKey) < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 bytes.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
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
builder.Services.AddHttpClient<IUserProfilesClient, UserProfilesClient>(client =>
{
    var baseAddress = builder.Configuration["UsersApi:BaseAddress"]
        ?? throw new InvalidOperationException("Configure UsersApi:BaseAddress.");
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
