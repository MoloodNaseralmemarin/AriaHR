using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AriaHR.Modules.Requests.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorLeaveRequestDatesAndCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Date",
                table: "LeaveRequests",
                newName: "StartDate");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "LeaveRequests",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<Guid>(
                name: "LeaveCategoryId",
                table: "LeaveRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("UPDATE LeaveRequests SET EndDate = StartDate;");

            migrationBuilder.Sql(@"
                UPDATE lr
                SET lr.LeaveCategoryId = lc.Id
                FROM LeaveRequests lr
                CROSS APPLY (
                    SELECT TOP(1) Id
                    FROM LeaveCategories
                    WHERE OrganizationId = lr.OrganizationId AND IsDeleted = 0
                    ORDER BY IsActive DESC, CreatedAtUtc ASC
                ) lc
                WHERE lr.LeaveCategoryId = '00000000-0000-0000-0000-000000000000';
            ");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_LeaveCategoryId",
                table: "LeaveRequests",
                column: "LeaveCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_LeaveCategoryId",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "LeaveCategoryId",
                table: "LeaveRequests");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "LeaveRequests",
                newName: "Date");
        }
    }
}
