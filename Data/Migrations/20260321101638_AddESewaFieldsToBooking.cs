using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixItNepal.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddESewaFieldsToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ESewaTransactionId",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTokenPaid",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TokenAmount",
                table: "Bookings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ESewaTransactionId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsTokenPaid",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TokenAmount",
                table: "Bookings");
        }
    }
}
