using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using YoutubeTool.Api.Data;
using YoutubeTool.Api.Options;
using YoutubeTool.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<YoutubeApiOptions>(builder.Configuration.GetSection("YoutubeApi"));

builder.Services.AddHttpClient<IYoutubeCommentService, YoutubeCommentService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<YoutubeApiOptions>>();
    client.BaseAddress = new Uri(options.Value.BaseUrl);
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Default");

app.UseAuthorization();

app.MapControllers();

app.Run();
