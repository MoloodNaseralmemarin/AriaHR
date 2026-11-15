using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AriaHR.Modules.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendWorkLocationQrAndGeofencing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QRCodes_WorkLocationId",
                table: "QRCodes");

            migrationBuilder.RenameColumn(
                name: "RadiusInMeters",
                table: "WorkLocations",
                newName: "AllowedRadiusInMeters");

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "WorkLocations",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "WorkLocations",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "AllowedRadiusInMeters",
                table: "WorkLocations",
                type: "decimal(8,1)",
                precision: 8,
                scale: 1,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "WorkLocations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "QrCodeEnabled",
                table: "WorkLocations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "QrCodeIdentifier",
                table: "WorkLocations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "QrCodeLastGeneratedAtUtc",
                table: "WorkLocations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QrCodeMode",
                table: "WorkLocations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QrRotationIntervalMinutes",
                table: "WorkLocations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "QRCodes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "QRCodes",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Backfill new configuration columns for existing work locations without losing data.
            // Existing records keep a dynamic QR with the default 5 minute rotation, which preserves
            // the previous behaviour of the per-location rotating QR endpoint.
            migrationBuilder.Sql(@"
UPDATE [WorkLocations]
SET [Name] = N'محل کار اصلی'
WHERE [Name] = '' OR [Name] IS NULL;
");

            migrationBuilder.Sql(@"
UPDATE [WorkLocations]
SET [QrCodeIdentifier] = LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '')),
    [QrCodeMode] = 2,
    [QrCodeEnabled] = CASE WHEN [IsActive] = 1 THEN 1 ELSE 0 END,
    [QrRotationIntervalMinutes] = 5,
    [QrCodeLastGeneratedAtUtc] = SYSUTCDATETIME()
WHERE [QrCodeIdentifier] = '' OR [QrCodeIdentifier] IS NULL;
");

            migrationBuilder.CreateIndex(
                name: "IX_WorkLocations_QrCodeIdentifier",
                table: "WorkLocations",
                column: "QrCodeIdentifier",
                unique: true,
                filter: "[QrCodeIdentifier] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_QRCodes_Code",
                table: "QRCodes",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_QRCodes_WorkLocationId_IsActive",
                table: "QRCodes",
                columns: new[] { "WorkLocationId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkLocations_QrCodeIdentifier",
                table: "WorkLocations");

            migrationBuilder.DropIndex(
                name: "IX_QRCodes_Code",
                table: "QRCodes");

            migrationBuilder.DropIndex(
                name: "IX_QRCodes_WorkLocationId_IsActive",
                table: "QRCodes");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "WorkLocations");

            migrationBuilder.DropColumn(
                name: "QrCodeEnabled",
                table: "WorkLocations");

            migrationBuilder.DropColumn(
                name: "QrCodeIdentifier",
                table: "WorkLocations");

            migrationBuilder.DropColumn(
                name: "QrCodeLastGeneratedAtUtc",
                table: "WorkLocations");

            migrationBuilder.DropColumn(
                name: "QrCodeMode",
                table: "WorkLocations");

            migrationBuilder.DropColumn(
                name: "QrRotationIntervalMinutes",
                table: "WorkLocations");

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "WorkLocations",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6);

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "WorkLocations",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6);

            migrationBuilder.AlterColumn<double>(
                name: "AllowedRadiusInMeters",
                table: "WorkLocations",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,1)",
                oldPrecision: 8,
                oldScale: 1);

            migrationBuilder.RenameColumn(
                name: "AllowedRadiusInMeters",
                table: "WorkLocations",
                newName: "RadiusInMeters");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "QRCodes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "QRCodes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.CreateIndex(
                name: "IX_QRCodes_WorkLocationId",
                table: "QRCodes",
                column: "WorkLocationId");
        }
    }
}
