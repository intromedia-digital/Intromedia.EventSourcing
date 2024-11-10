using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventSouring.Sample.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StreamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Published = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Event_Published",
                table: "Event",
                column: "Published");

            migrationBuilder.CreateIndex(
                name: "IX_Event_StreamId",
                table: "Event",
                column: "StreamId");

            migrationBuilder.CreateIndex(
                name: "IX_Event_StreamId_Timestamp",
                table: "Event",
                columns: new[] { "StreamId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Event_StreamId_Version",
                table: "Event",
                columns: new[] { "StreamId", "Version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Event");
        }
    }
}
