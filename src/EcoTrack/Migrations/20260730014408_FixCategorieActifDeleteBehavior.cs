using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoTrack.Migrations
{
    /// <inheritdoc />
    public partial class FixCategorieActifDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actifs_CategoriesActifs_CategorieActifId",
                table: "Actifs");

            migrationBuilder.AddForeignKey(
                name: "FK_Actifs_CategoriesActifs_CategorieActifId",
                table: "Actifs",
                column: "CategorieActifId",
                principalTable: "CategoriesActifs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actifs_CategoriesActifs_CategorieActifId",
                table: "Actifs");

            migrationBuilder.AddForeignKey(
                name: "FK_Actifs_CategoriesActifs_CategorieActifId",
                table: "Actifs",
                column: "CategorieActifId",
                principalTable: "CategoriesActifs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
