using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AriaHR.Modules.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkLocationQrCodeModes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QRCodes_WorkLocationId",
                table: "QRCodes");

            migrationBuilder.AddColumn<int>(
                name: "QrCodeMode",
                table: "WorkLocations",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<string>(
                name: "QrCodeReference",
                table: "WorkLocations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QrCodeRotationMinutes",
                table: "WorkLocations",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<string>(
                name: "QrCodeSecret",
                table: "WorkLocations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "QrCodeVerificationEnabled",
                table: "WorkLocations",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "QRCodes",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_WorkLocations_QrCodeReference",
                table: "WorkLocations",
                column: "QrCodeReference",
                unique: true,
                filter: "[QrCodeReference] IS NOT NULL");

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
                name: "IX_WorkLocations_QrCodeReference",
                table: "WorkLocations");

            migrationBuilder.DropIndex(
                name: "IX_QRCodes_Code",
                table: "QRCodes");

            migrationBuilder.DropIndex(
                name: "IX_QRCodes_WorkLocationId_IsActive",
                table: "QRCodes");

            migrationBuilder.DropColumn(
                name: "QrCodeMode",
                table: "WorkLocations");

            migrationBuilder.DropColumn(
                name: "QrCodeReference",
                table: "WorkLocations");

            migrationBuilder.DropColumn(
                name: "QrCodeRotationMinutes",
                table: "WorkLocations");

            migrationBuilder.DropColumn(
                name: "QrCodeSecret",
                table: "WorkLocations");

            migrationBuilder.DropColumn(
                name: "QrCodeVerificationEnabled",
                table: "WorkLocations");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "QRCodes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.CreateIndex(
                name: "IX_QRCodes_WorkLocationId",
                table: "QRCodes",
                column: "WorkLocationId");
        }
    }
}
