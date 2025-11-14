using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using YoutubeTool.Api.Data;

#nullable disable

namespace YoutubeTool.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            modelBuilder.Entity("YoutubeTool.Api.Models.ApiRequestLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasAnnotation("Sqlite:Autoincrement", true);

                    b.Property<int>("RequestedPage")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ReturnedCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("VideoId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("TimestampUtc")
                        .HasColumnType("TEXT");

                    b.Property<string>("RequestIp")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("ApiRequestLogs");
                });

            modelBuilder.Entity("YoutubeTool.Api.Models.YoutubeComment", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasAnnotation("Sqlite:Autoincrement", true);

                    b.Property<string>("AuthorChannelId")
                        .HasMaxLength(128)
                        .HasColumnType("TEXT");

                    b.Property<string>("AuthorChannelUrl")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("TEXT");

                    b.Property<string>("AuthorDisplayName")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("CommentId")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("TEXT");

                    b.Property<string>("CommentText")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("PublishedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("RetrievedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("VideoId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("VideoId", "CommentId")
                        .IsUnique();

                    b.HasIndex("VideoId", "PublishedAt");

                    b.ToTable("YoutubeComments");
                });
#pragma warning restore 612, 618
        }
    }
}
