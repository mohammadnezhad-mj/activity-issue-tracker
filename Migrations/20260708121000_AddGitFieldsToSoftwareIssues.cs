using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RayanTask.Data;

#nullable disable

namespace RayanTask.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260708121000_AddGitFieldsToSoftwareIssues")]
    public partial class AddGitFieldsToSoftwareIssues : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BranchName",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepositoryUrl",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommitUrl",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkItemType",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitStatus",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BranchName",
                table: "SoftwareIssues");

            migrationBuilder.DropColumn(
                name: "RepositoryUrl",
                table: "SoftwareIssues");

            migrationBuilder.DropColumn(
                name: "CommitUrl",
                table: "SoftwareIssues");

            migrationBuilder.DropColumn(
                name: "WorkItemType",
                table: "SoftwareIssues");

            migrationBuilder.DropColumn(
                name: "GitStatus",
                table: "SoftwareIssues");
        }
    }
}
