using Microsoft.EntityFrameworkCore;
using YoutubeTool.Api.Models;

namespace YoutubeTool.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApiRequestLog> ApiRequestLogs => Set<ApiRequestLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApiRequestLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VideoId).IsRequired();
            entity.Property(e => e.TimestampUtc).IsRequired();
            entity.Property(e => e.RequestedPage).IsRequired();
            entity.Property(e => e.ReturnedCount).IsRequired();
        });
    }
}
