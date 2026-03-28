using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixItNepal.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCounterBargainingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerCounterMessage",
                table: "ServiceBids",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomerCounterPrice",
                table: "ServiceBids",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerCounterMessage",
                table: "ServiceBids");

            migrationBuilder.DropColumn(
                name: "CustomerCounterPrice",
                table: "ServiceBids");
        }
    }
}
