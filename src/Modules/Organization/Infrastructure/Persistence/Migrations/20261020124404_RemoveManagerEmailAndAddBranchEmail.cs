using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AriaHR.Modules.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveManagerEmailAndAddBranchEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManagerEmail",
                table: "Organizations");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Organizations",
                newName: "OrganizationType");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Branches",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Branches");

            migrationBuilder.RenameColumn(
                name: "OrganizationType",
                table: "Organizations",
                newName: "Type");

            migrationBuilder.AddColumn<string>(
                name: "ManagerEmail",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
