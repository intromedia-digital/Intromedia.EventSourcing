using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventSourcing.Sample.Migrations
{
    /// <inheritdoc />
    public partial class EventStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Projections");

            migrationBuilder.CreateTable(
                name: "ReceivedEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivedEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedEvents_EventId",
                table: "ReceivedEvents",
                column: "EventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceivedEvents");

            migrationBuilder.CreateTable(
                name: "Projections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Offset = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    StreamType = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projections_Name",
                table: "Projections",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Projections_StreamType",
                table: "Projections",
                column: "StreamType");
        }
    }
}
