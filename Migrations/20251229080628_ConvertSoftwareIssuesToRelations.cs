using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RayanTask.Migrations
{
    /// <inheritdoc />
    public partial class ConvertSoftwareIssuesToRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add new columns as nullable first
            migrationBuilder.AddColumn<int>(
                name: "AssignedToId",
                table: "SoftwareIssues",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IssueTypeId",
                table: "SoftwareIssues",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReporterId",
                table: "SoftwareIssues",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoftwareNameId",
                table: "SoftwareIssues",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "SoftwareIssues",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Statuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statuses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareIssues_AssignedToId",
                table: "SoftwareIssues",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareIssues_IssueTypeId",
                table: "SoftwareIssues",
                column: "IssueTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareIssues_ReporterId",
                table: "SoftwareIssues",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareIssues_SoftwareNameId",
                table: "SoftwareIssues",
                column: "SoftwareNameId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareIssues_StatusId",
                table: "SoftwareIssues",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Statuses_Value",
                table: "Statuses",
                column: "Value",
                unique: true);

            // Step 2: Seed default statuses
            migrationBuilder.Sql(@"
                INSERT OR IGNORE INTO Statuses (Value, IsActive, CreatedAt)
                VALUES 
                    ('ثبت شده', 1, datetime('now')),
                    ('در حال بررسی', 1, datetime('now')),
                    ('در حال انجام', 1, datetime('now')),
                    ('تکمیل شده', 1, datetime('now')),
                    ('لغو شده', 1, datetime('now'));
            ");

            // Step 3: Migrate data from old columns to new columns
            // Migrate SoftwareName
            migrationBuilder.Sql(@"
                UPDATE SoftwareIssues
                SET SoftwareNameId = (
                    SELECT Id FROM SoftwareNames 
                    WHERE SoftwareNames.Value = SoftwareIssues.SoftwareName 
                    LIMIT 1
                )
                WHERE SoftwareNameId IS NULL;
            ");

            // Migrate IssueType
            migrationBuilder.Sql(@"
                UPDATE SoftwareIssues
                SET IssueTypeId = (
                    SELECT Id FROM IssueTypes 
                    WHERE IssueTypes.Value = SoftwareIssues.IssueType 
                    LIMIT 1
                )
                WHERE IssueTypeId IS NULL;
            ");

            // Migrate Status
            migrationBuilder.Sql(@"
                UPDATE SoftwareIssues
                SET StatusId = (
                    SELECT Id FROM Statuses 
                    WHERE Statuses.Value = SoftwareIssues.Status 
                    LIMIT 1
                )
                WHERE StatusId IS NULL;
            ");

            // Migrate Reporter
            migrationBuilder.Sql(@"
                UPDATE SoftwareIssues
                SET ReporterId = (
                    SELECT Id FROM Users 
                    WHERE Users.FirstName = SoftwareIssues.ReporterFirstName 
                    AND Users.LastName = SoftwareIssues.ReporterLastName
                    LIMIT 1
                )
                WHERE ReporterId IS NULL;
            ");

            // Migrate AssignedTo
            migrationBuilder.Sql(@"
                UPDATE SoftwareIssues
                SET AssignedToId = (
                    SELECT Id FROM Users 
                    WHERE Users.FirstName = SoftwareIssues.AssignedToFirstName 
                    AND Users.LastName = SoftwareIssues.AssignedToLastName
                    LIMIT 1
                )
                WHERE AssignedToId IS NULL AND AssignedToFirstName IS NOT NULL AND AssignedToLastName IS NOT NULL;
            ");

            // Step 4: Set default values for records that couldn't be migrated
            migrationBuilder.Sql(@"
                UPDATE SoftwareIssues
                SET SoftwareNameId = (SELECT Id FROM SoftwareNames LIMIT 1)
                WHERE SoftwareNameId IS NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE SoftwareIssues
                SET IssueTypeId = (SELECT Id FROM IssueTypes LIMIT 1)
                WHERE IssueTypeId IS NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE SoftwareIssues
                SET StatusId = (SELECT Id FROM Statuses WHERE Value = 'ثبت شده' LIMIT 1)
                WHERE StatusId IS NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE SoftwareIssues
                SET ReporterId = (SELECT Id FROM Users LIMIT 1)
                WHERE ReporterId IS NULL;
            ");

            // Step 5: Make columns non-nullable (except AssignedToId)
            migrationBuilder.AlterColumn<int>(
                name: "IssueTypeId",
                table: "SoftwareIssues",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ReporterId",
                table: "SoftwareIssues",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SoftwareNameId",
                table: "SoftwareIssues",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "StatusId",
                table: "SoftwareIssues",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            // Step 6: Drop old columns
            migrationBuilder.DropColumn(
                name: "AssignedToFirstName",
                table: "SoftwareIssues");

            migrationBuilder.DropColumn(
                name: "AssignedToLastName",
                table: "SoftwareIssues");

            migrationBuilder.DropColumn(
                name: "IssueType",
                table: "SoftwareIssues");

            migrationBuilder.DropColumn(
                name: "ReporterFirstName",
                table: "SoftwareIssues");

            migrationBuilder.DropColumn(
                name: "ReporterLastName",
                table: "SoftwareIssues");

            migrationBuilder.DropColumn(
                name: "SoftwareName",
                table: "SoftwareIssues");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "SoftwareIssues");

            migrationBuilder.AddForeignKey(
                name: "FK_SoftwareIssues_IssueTypes_IssueTypeId",
                table: "SoftwareIssues",
                column: "IssueTypeId",
                principalTable: "IssueTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SoftwareIssues_SoftwareNames_SoftwareNameId",
                table: "SoftwareIssues",
                column: "SoftwareNameId",
                principalTable: "SoftwareNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SoftwareIssues_Statuses_StatusId",
                table: "SoftwareIssues",
                column: "StatusId",
                principalTable: "Statuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SoftwareIssues_Users_AssignedToId",
                table: "SoftwareIssues",
                column: "AssignedToId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SoftwareIssues_Users_ReporterId",
                table: "SoftwareIssues",
                column: "ReporterId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SoftwareIssues_IssueTypes_IssueTypeId",
                table: "SoftwareIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_SoftwareIssues_SoftwareNames_SoftwareNameId",
                table: "SoftwareIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_SoftwareIssues_Statuses_StatusId",
                table: "SoftwareIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_SoftwareIssues_Users_AssignedToId",
                table: "SoftwareIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_SoftwareIssues_Users_ReporterId",
                table: "SoftwareIssues");

            migrationBuilder.DropTable(
                name: "Statuses");

            migrationBuilder.DropIndex(
                name: "IX_SoftwareIssues_AssignedToId",
                table: "SoftwareIssues");

            migrationBuilder.DropIndex(
                name: "IX_SoftwareIssues_IssueTypeId",
                table: "SoftwareIssues");

            migrationBuilder.DropIndex(
                name: "IX_SoftwareIssues_ReporterId",
                table: "SoftwareIssues");

            migrationBuilder.DropIndex(
                name: "IX_SoftwareIssues_SoftwareNameId",
                table: "SoftwareIssues");

            migrationBuilder.DropIndex(
                name: "IX_SoftwareIssues_StatusId",
                table: "SoftwareIssues");

            migrationBuilder.DropColumn(
                name: "AssignedToId",
                table: "SoftwareIssues");

            migrationBuilder.DropColumn(
                name: "IssueTypeId",
                table: "SoftwareIssues");

            migrationBuilder.DropColumn(
                name: "ReporterId",
                table: "SoftwareIssues");

            migrationBuilder.DropColumn(
                name: "SoftwareNameId",
                table: "SoftwareIssues");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "SoftwareIssues");

            migrationBuilder.AddColumn<string>(
                name: "AssignedToFirstName",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedToLastName",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssueType",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReporterFirstName",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReporterLastName",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SoftwareName",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "SoftwareIssues",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
