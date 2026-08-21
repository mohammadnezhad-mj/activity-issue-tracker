using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RayanTask.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerNameToSoftwareIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "SoftwareIssues");
        }
    }
}
