using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoTrack.Migrations
{
    /// <inheritdoc />
    public partial class AjoutAttributionUnite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UniteId",
                table: "Actifs",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Actifs_UniteId",
                table: "Actifs",
                column: "UniteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Actifs_Unites_UniteId",
                table: "Actifs",
                column: "UniteId",
                principalTable: "Unites",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actifs_Unites_UniteId",
                table: "Actifs");

            migrationBuilder.DropIndex(
                name: "IX_Actifs_UniteId",
                table: "Actifs");

            migrationBuilder.DropColumn(
                name: "UniteId",
                table: "Actifs");
        }
    }
}
