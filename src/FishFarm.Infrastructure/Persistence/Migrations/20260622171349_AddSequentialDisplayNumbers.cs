using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishFarm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSequentialDisplayNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkerNumber",
                table: "Workers",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "FarmNumber",
                table: "FishFarms",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.CreateIndex(
                name: "UIX_Workers_WorkerNumber",
                table: "Workers",
                column: "WorkerNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UIX_FishFarms_FarmNumber",
                table: "FishFarms",
                column: "FarmNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UIX_Workers_WorkerNumber",
                table: "Workers");

            migrationBuilder.DropIndex(
                name: "UIX_FishFarms_FarmNumber",
                table: "FishFarms");

            migrationBuilder.DropColumn(
                name: "WorkerNumber",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "FarmNumber",
                table: "FishFarms");
        }
    }
}
