using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishFarm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropWorkersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Workers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Workers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FishFarmId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    CertifiedUntil = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PicturePublicId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PictureUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    WorkerNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workers_FishFarms_FishFarmId",
                        column: x => x.FishFarmId,
                        principalTable: "FishFarms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Workers_FishFarmId",
                table: "Workers",
                column: "FishFarmId");

            migrationBuilder.CreateIndex(
                name: "UIX_Workers_Email_Active",
                table: "Workers",
                column: "Email",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UIX_Workers_WorkerNumber",
                table: "Workers",
                column: "WorkerNumber",
                unique: true);
        }
    }
}
