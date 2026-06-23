using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishFarm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPeopleAndFarmWorkers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workers_FishFarms_FishFarmId",
                table: "Workers");

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    CertifiedUntil = table.Column<DateOnly>(type: "date", nullable: false),
                    PictureUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PicturePublicId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FarmWorkers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FishFarmId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmWorkers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmWorkers_FishFarms_FishFarmId",
                        column: x => x.FishFarmId,
                        principalTable: "FishFarms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FarmWorkers_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FarmWorkers_FarmPosition",
                table: "FarmWorkers",
                columns: new[] { "FishFarmId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_FarmWorkers_FishFarmId",
                table: "FarmWorkers",
                column: "FishFarmId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmWorkers_PersonId",
                table: "FarmWorkers",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "UIX_FarmWorkers_FarmPerson_Active",
                table: "FarmWorkers",
                columns: new[] { "FishFarmId", "PersonId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UIX_People_Email_Active",
                table: "People",
                column: "Email",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UIX_People_PersonNumber",
                table: "People",
                column: "PersonNumber",
                unique: true);

            // ── Data Migration: Workers → People + FarmWorkers ─────────────────────
            // Migrate existing worker profiles into the People table,
            // preserving the same Id so any external references remain valid.
            migrationBuilder.Sql(@"
                INSERT INTO People
                    (Id, Name, Email, Age, CertifiedUntil,
                     PictureUrl, PicturePublicId,
                     IsDeleted, DeletedAt, DeletedBy,
                     CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
                SELECT
                    Id, Name, Email, Age, CertifiedUntil,
                    PictureUrl, PicturePublicId,
                    IsDeleted, DeletedAt, DeletedBy,
                    CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
                FROM Workers
            ");

            // Migrate farm assignments into FarmWorkers, generating new Guids for each row.
            migrationBuilder.Sql(@"
                INSERT INTO FarmWorkers
                    (Id, FishFarmId, PersonId, Position,
                     IsDeleted, DeletedAt, DeletedBy,
                     CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
                SELECT
                    NEWID(), FishFarmId, Id, Position,
                    IsDeleted, DeletedAt, DeletedBy,
                    CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
                FROM Workers
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_FishFarms_FishFarmId",
                table: "Workers",
                column: "FishFarmId",
                principalTable: "FishFarms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workers_FishFarms_FishFarmId",
                table: "Workers");

            migrationBuilder.DropTable(
                name: "FarmWorkers");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_FishFarms_FishFarmId",
                table: "Workers",
                column: "FishFarmId",
                principalTable: "FishFarms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
