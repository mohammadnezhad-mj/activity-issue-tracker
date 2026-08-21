using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RayanTask.Migrations
{
    /// <inheritdoc />
    public partial class ConvertToRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add new columns as nullable first
            migrationBuilder.AddColumn<int>(
                name: "ResultId",
                table: "ActivityRecords",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "ActivityRecords",
                type: "INTEGER",
                nullable: true);

            // Step 2: Create ActivityRecordActivityType table first
            migrationBuilder.CreateTable(
                name: "ActivityRecordActivityType",
                columns: table => new
                {
                    ActivityRecordId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivityTypeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityRecordActivityType", x => new { x.ActivityRecordId, x.ActivityTypeId });
                    table.ForeignKey(
                        name: "FK_ActivityRecordActivityType_ActivityRecords_ActivityRecordId",
                        column: x => x.ActivityRecordId,
                        principalTable: "ActivityRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityRecordActivityType_ActivityTypes_ActivityTypeId",
                        column: x => x.ActivityTypeId,
                        principalTable: "ActivityTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Step 3: Migrate data from old columns to new columns
            // Migrate Name/LastName to UserId
            migrationBuilder.Sql(@"
                UPDATE ActivityRecords
                SET UserId = (
                    SELECT Id FROM Users 
                    WHERE Users.FirstName = ActivityRecords.Name 
                    AND Users.LastName = ActivityRecords.LastName
                    LIMIT 1
                )
                WHERE UserId IS NULL;
            ");

            // Migrate Result to ResultId
            migrationBuilder.Sql(@"
                UPDATE ActivityRecords
                SET ResultId = (
                    SELECT Id FROM Results 
                    WHERE Results.Value = ActivityRecords.Result
                    LIMIT 1
                )
                WHERE ResultId IS NULL;
            ");

            // Migrate ActivityType to ActivityRecordActivityType
            migrationBuilder.Sql(@"
                INSERT INTO ActivityRecordActivityType (ActivityRecordId, ActivityTypeId)
                SELECT ar.Id, at.Id
                FROM ActivityRecords ar
                CROSS JOIN ActivityTypes at
                WHERE at.Value = ar.ActivityType
                AND NOT EXISTS (
                    SELECT 1 FROM ActivityRecordActivityType arat
                    WHERE arat.ActivityRecordId = ar.Id AND arat.ActivityTypeId = at.Id
                );
            ");

            // Step 4: Update NULL values to default (for records that couldn't be migrated)
            migrationBuilder.Sql(@"
                UPDATE ActivityRecords
                SET UserId = (SELECT Id FROM Users LIMIT 1)
                WHERE UserId IS NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE ActivityRecords
                SET ResultId = (SELECT Id FROM Results LIMIT 1)
                WHERE ResultId IS NULL;
            ");

            // Step 5: Drop old columns
            migrationBuilder.DropColumn(
                name: "ActivityType",
                table: "ActivityRecords");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "ActivityRecords");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ActivityRecords");

            migrationBuilder.DropColumn(
                name: "Result",
                table: "ActivityRecords");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityRecords_ResultId",
                table: "ActivityRecords",
                column: "ResultId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityRecords_UserId",
                table: "ActivityRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityRecordActivityType_ActivityTypeId",
                table: "ActivityRecordActivityType",
                column: "ActivityTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityRecords_Results_ResultId",
                table: "ActivityRecords",
                column: "ResultId",
                principalTable: "Results",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityRecords_Users_UserId",
                table: "ActivityRecords",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityRecords_Results_ResultId",
                table: "ActivityRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ActivityRecords_Users_UserId",
                table: "ActivityRecords");

            migrationBuilder.DropTable(
                name: "ActivityRecordActivityType");

            migrationBuilder.DropIndex(
                name: "IX_ActivityRecords_ResultId",
                table: "ActivityRecords");

            migrationBuilder.DropIndex(
                name: "IX_ActivityRecords_UserId",
                table: "ActivityRecords");

            migrationBuilder.DropColumn(
                name: "ResultId",
                table: "ActivityRecords");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ActivityRecords");

            migrationBuilder.AddColumn<string>(
                name: "ActivityType",
                table: "ActivityRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "ActivityRecords",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ActivityRecords",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Result",
                table: "ActivityRecords",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
