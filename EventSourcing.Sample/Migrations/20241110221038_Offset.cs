using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventSourcing.Sample.Migrations
{
    /// <inheritdoc />
    public partial class Offset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContinuationToken",
                table: "Projections");

            migrationBuilder.AddColumn<int>(
                name: "Offset",
                table: "Projections",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Offset",
                table: "Projections");

            migrationBuilder.AddColumn<string>(
                name: "ContinuationToken",
                table: "Projections",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
