using Microsoft.Extensions.Options;
using YoutubeTool.Api.Options;
using YoutubeTool.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

builder.Services.AddSingleton<IApiRequestLogger, ApiRequestLogger>();

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
