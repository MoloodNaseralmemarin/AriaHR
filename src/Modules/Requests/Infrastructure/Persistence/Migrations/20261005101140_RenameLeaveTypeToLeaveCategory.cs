using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AriaHR.Modules.Requests.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameLeaveTypeToLeaveCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveBalances_LeaveTypes_LeaveTypeId",
                table: "LeaveBalances");

            migrationBuilder.RenameTable(
                name: "LeaveTypes",
                newName: "LeaveCategories");

            migrationBuilder.RenameColumn(
                name: "LeaveTypeId",
                table: "LeaveBalances",
                newName: "LeaveCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveBalances_LeaveTypeId",
                table: "LeaveBalances",
                newName: "IX_LeaveBalances_LeaveCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveBalances_LeaveCategories_LeaveCategoryId",
                table: "LeaveBalances",
                column: "LeaveCategoryId",
                principalTable: "LeaveCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveBalances_LeaveCategories_LeaveCategoryId",
                table: "LeaveBalances");

            migrationBuilder.RenameTable(
                name: "LeaveCategories",
                newName: "LeaveTypes");

            migrationBuilder.RenameColumn(
                name: "LeaveCategoryId",
                table: "LeaveBalances",
                newName: "LeaveTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_LeaveBalances_LeaveCategoryId",
                table: "LeaveBalances",
                newName: "IX_LeaveBalances_LeaveTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveBalances_LeaveTypes_LeaveTypeId",
                table: "LeaveBalances",
                column: "LeaveTypeId",
                principalTable: "LeaveTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
