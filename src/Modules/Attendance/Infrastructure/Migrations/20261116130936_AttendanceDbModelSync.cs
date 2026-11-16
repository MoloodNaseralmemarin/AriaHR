using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AriaHR.Modules.Attendance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AttendanceDbModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckInLatitude = table.Column<double>(type: "float", nullable: true),
                    CheckInLongitude = table.Column<double>(type: "float", nullable: true),
                    CheckOutLatitude = table.Column<double>(type: "float", nullable: true),
                    CheckOutLongitude = table.Column<double>(type: "float", nullable: true),
                    CheckInAccuracy = table.Column<double>(type: "float", nullable: true),
                    CheckOutAccuracy = table.Column<double>(type: "float", nullable: true),
                    CheckInDistanceInMeters = table.Column<double>(type: "float", nullable: true),
                    CheckOutDistanceInMeters = table.Column<double>(type: "float", nullable: true),
                    CheckInLocationTimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckOutLocationTimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckInIdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CheckOutIdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WorkLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QRCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsWithinGeofence = table.Column<bool>(type: "bit", nullable: false),
                    DeviceInfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_EmployeeId_CheckInIdempotencyKey",
                table: "AttendanceRecords",
                columns: new[] { "EmployeeId", "CheckInIdempotencyKey" },
                unique: true,
                filter: "[CheckInIdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_EmployeeId_CheckOutIdempotencyKey",
                table: "AttendanceRecords",
                columns: new[] { "EmployeeId", "CheckOutIdempotencyKey" },
                unique: true,
                filter: "[CheckOutIdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_EmployeeId_CheckOutTime",
                table: "AttendanceRecords",
                columns: new[] { "EmployeeId", "CheckOutTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_OrganizationId_EmployeeId_CheckInTime",
                table: "AttendanceRecords",
                columns: new[] { "OrganizationId", "EmployeeId", "CheckInTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceRecords");
        }
    }
}
