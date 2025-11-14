using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeTool.Api.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiRequestLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VideoId = table.Column<string>(type: "TEXT", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RequestedPage = table.Column<int>(type: "INTEGER", nullable: false),
                    ReturnedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestIp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiRequestLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "YoutubeComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VideoId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CommentId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    AuthorChannelId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    AuthorDisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AuthorChannelUrl = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    CommentText = table.Column<string>(type: "TEXT", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RetrievedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YoutubeComments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YoutubeComments_VideoId_CommentId",
                table: "YoutubeComments",
                columns: new[] { "VideoId", "CommentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YoutubeComments_VideoId_PublishedAt",
                table: "YoutubeComments",
                columns: new[] { "VideoId", "PublishedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiRequestLogs");

            migrationBuilder.DropTable(
                name: "YoutubeComments");
        }
    }
}
