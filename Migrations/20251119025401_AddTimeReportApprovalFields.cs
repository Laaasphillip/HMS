using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeReportApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalNotes",
                table: "TimeReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "TimeReports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "TimeReports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "TimeReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EarlyDepartureMinutes",
                table: "TimeReports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LateArrivalMinutes",
                table: "TimeReports",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalNotes",
                table: "TimeReports");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "TimeReports");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "TimeReports");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "TimeReports");

            migrationBuilder.DropColumn(
                name: "EarlyDepartureMinutes",
                table: "TimeReports");

            migrationBuilder.DropColumn(
                name: "LateArrivalMinutes",
                table: "TimeReports");
        }
    }
}
