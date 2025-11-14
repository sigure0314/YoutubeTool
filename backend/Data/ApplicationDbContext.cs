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
    public DbSet<YoutubeComment> YoutubeComments => Set<YoutubeComment>();

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

        modelBuilder.Entity<YoutubeComment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VideoId).IsRequired();
            entity.Property(e => e.CommentId).IsRequired();
            entity.Property(e => e.AuthorDisplayName).IsRequired();
            entity.Property(e => e.AuthorChannelUrl).IsRequired();
            entity.Property(e => e.CommentText).IsRequired();
            entity.Property(e => e.PublishedAt).IsRequired();
            entity.Property(e => e.RetrievedAt).IsRequired();

            entity.HasIndex(e => new { e.VideoId, e.CommentId }).IsUnique();
            entity.HasIndex(e => new { e.VideoId, e.PublishedAt });
        });
    }
}
