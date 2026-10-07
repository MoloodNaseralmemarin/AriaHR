using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AriaHR.Modules.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreEmployeeProfileImageInDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImagePath",
                table: "Employees");

            migrationBuilder.AddColumn<byte[]>(
                name: "ProfileImage",
                table: "Employees",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageContentType",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageFileName",
                table: "Employees",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImage",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ProfileImageContentType",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ProfileImageFileName",
                table: "Employees");

            migrationBuilder.AddColumn<string>(
                name: "ProfileImagePath",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
