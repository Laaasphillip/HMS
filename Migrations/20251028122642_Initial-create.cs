using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Migrations
{
    /// <inheritdoc />
    public partial class Initialcreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeReports_Schedules_ScheduleId",
                table: "TimeReports");

            migrationBuilder.DropIndex(
                name: "IX_TimeReports_ScheduleId",
                table: "TimeReports");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Patients");

            migrationBuilder.CreateIndex(
                name: "IX_TimeReports_ScheduleId",
                table: "TimeReports",
                column: "ScheduleId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeReports_Schedules_ScheduleId",
                table: "TimeReports",
                column: "ScheduleId",
                principalTable: "Schedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeReports_Schedules_ScheduleId",
                table: "TimeReports");

            migrationBuilder.DropIndex(
                name: "IX_TimeReports_ScheduleId",
                table: "TimeReports");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TimeReports_ScheduleId",
                table: "TimeReports",
                column: "ScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeReports_Schedules_ScheduleId",
                table: "TimeReports",
                column: "ScheduleId",
                principalTable: "Schedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
