using System.Text;
using FinanceTracker.Api.Middleware;
using FinanceTracker.Application.Services;
using FinanceTracker.Domain.Repositories;
using FinanceTracker.Infrastructure.Data;
using FinanceTracker.Infrastructure.Migrations;
using FinanceTracker.Infrastructure.Repositories;
using FinanceTracker.Infrastructure.Seeding;
using FinanceTracker.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var isTesting = builder.Environment.IsEnvironment("Testing");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secret = jwtSection["Secret"];
        if (string.IsNullOrEmpty(secret) && !isTesting)
        {
            throw new InvalidOperationException(
                "Jwt:Secret is not configured. Set the JWT__Secret environment variable before running.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrEmpty(jwtSection["Issuer"]),
            ValidateAudience = !string.IsNullOrEmpty(jwtSection["Audience"]),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = !string.IsNullOrEmpty(secret),
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = string.IsNullOrEmpty(secret)
                ? null
                : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtService, JwtService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

builder.Services.AddScoped<DatabaseSeeder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseMiddleware<CookieTokenMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (!isTesting)
{
    using var scope = app.Services.CreateScope();
    var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
    await MigrationRunner.ApplyAsync(connectionFactory);

    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();

public partial class Program { }
