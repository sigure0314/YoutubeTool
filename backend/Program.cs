using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YoutubeTool.Api.Data;
using YoutubeTool.Api.Options;
using YoutubeTool.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Default SQLite connection string is not configured.");
}

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();

builder.Services.Configure<YoutubeApiOptions>(builder.Configuration.GetSection("YoutubeApi"));

builder.Services.AddHttpClient<IYoutubeCommentService, YoutubeCommentService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<YoutubeApiOptions>>();
    var baseUrl = options.Value.BaseUrl?.TrimEnd('/') ?? string.Empty;

    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        throw new InvalidOperationException("YouTube API base URL is not configured.");
    }

    client.BaseAddress = new Uri($"{baseUrl}/", UriKind.Absolute);
});

builder.Services.AddScoped<IApiRequestLogger, ApiRequestLogger>();

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
    await initializer.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("Default");

app.UseAuthorization();

app.MapControllers();

app.Run();
