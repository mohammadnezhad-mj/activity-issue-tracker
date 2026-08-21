using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RayanTask.Data;

#nullable disable

namespace RayanTask.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260708122000_AddOperationalFieldsToSoftwareIssues")]
    public partial class AddOperationalFieldsToSoftwareIssues : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "SoftwareIssues",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Environment",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoftwareVersion",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModuleName",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportedChannel",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Priority", table: "SoftwareIssues");
            migrationBuilder.DropColumn(name: "Severity", table: "SoftwareIssues");
            migrationBuilder.DropColumn(name: "DueDate", table: "SoftwareIssues");
            migrationBuilder.DropColumn(name: "Environment", table: "SoftwareIssues");
            migrationBuilder.DropColumn(name: "SoftwareVersion", table: "SoftwareIssues");
            migrationBuilder.DropColumn(name: "ModuleName", table: "SoftwareIssues");
            migrationBuilder.DropColumn(name: "ReportedChannel", table: "SoftwareIssues");
        }
    }
}
