using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thread.API.Migrations
{
    /// <inheritdoc />
    public partial class AddThreadReplyMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReplyTimestamp",
                table: "Threads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReplyCount",
                table: "Threads",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReplyTimestamp",
                table: "Threads");

            migrationBuilder.DropColumn(
                name: "ReplyCount",
                table: "Threads");
        }
    }
}
