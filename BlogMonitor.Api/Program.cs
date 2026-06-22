using BlogMonitor.Infrastructure.Data;
using BlogMonitor.Infrastructure.Services;
using BlogMonitor.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
// using Microsoft.OpenApi.Models; // simplified to avoid dependency resolution issues
using System.Text;
using Hangfire;
using Hangfire.SqlServer;
using Hangfire.Dashboard;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
    "http://127.0.0.1:5173",
    "http://localhost:5174",
    "http://127.0.0.1:5174",
    "http://localhost:5175",
    "http://127.0.0.1:5175"
                )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// JWT Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
var issuer = jwtSection["Issuer"];
var audience = jwtSection["Audience"];
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Register DuckDB context as a singleton so the same connection is shared
builder.Services.AddSingleton<BlogMonitor.Infrastructure.DuckDb.DuckDbContext>();
// Register orchestrator and job for Hangfire
builder.Services.AddScoped<BlogMonitor.Infrastructure.Services.DailyPipelineOrchestrator>();
builder.Services.AddScoped<BlogMonitor.Infrastructure.Jobs.DailyPipelineJob>();

// Register database initializer to create DuckDB tables and apply EF migrations on startup
builder.Services.AddHostedService<BlogMonitor.Infrastructure.Data.DatabaseInitializer>();

// Register pipeline and supporting services
builder.Services.AddScoped<ISitemapDiscoveryService, SitemapDiscoveryService>();
// SitemapDownloaderService registration removed temporarily to avoid interface resolution build issues
builder.Services.AddScoped<SitemapDownloaderService>();
builder.Services.AddScoped<ITrackBCrawlerService, TrackBCrawlerService>();
builder.Services.AddScoped<IPlaywrightScraperService, PlaywrightScraperService>();
builder.Services.AddScoped<IContentExtractorService, ContentExtractorService>();
builder.Services.AddScoped<IContentHashService, ContentHashService>();
builder.Services.AddScoped<IMarkdownWriterService, MarkdownWriterService>();
builder.Services.AddScoped<ITfidfSummariserService, TfidfSummariserService>();
builder.Services.AddScoped<IDuckDbReportService, DuckDbReportService>();
builder.Services.AddScoped<IWeeklyReportGeneratorService, WeeklyReportGeneratorService>();
builder.Services.AddScoped<IOutlookEmailSenderService, OutlookEmailSenderService>();
builder.Services.AddScoped<IReportQueryService, ReportQueryService>();

// Hangfire
builder.Services.AddHangfire(config => config.UseSqlServerStorage(builder.Configuration.GetConnectionString("SqlServer")));
builder.Services.AddHangfireServer(options => options.WorkerCount = 1);

// Weekly cycle service depends on scoped DB context — use scoped lifetime
builder.Services.AddScoped<IWeeklyCycleService, WeeklyCycleService>();

// Client services
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IClientUrlService, ClientUrlService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseHttpsRedirection();
app.UseCors("DefaultCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

// Hangfire dashboard with simple admin-only authorization
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAdminFilter() }
});

app.MapControllers();

// Seed roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Admin", "Viewer" };
    foreach (var r in roles)
    {
        var exists = await roleManager.RoleExistsAsync(r);
        if (!exists)
        {
            await roleManager.CreateAsync(new IdentityRole(r));
        }
    }
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        )).ToArray();

    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
