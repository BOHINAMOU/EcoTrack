using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoTrack.Migrations
{
    /// <inheritdoc />
    public partial class AjoutCompteEmploye : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Employes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employes_ApplicationUserId",
                table: "Employes",
                column: "ApplicationUserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Employes_AspNetUsers_ApplicationUserId",
                table: "Employes",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employes_AspNetUsers_ApplicationUserId",
                table: "Employes");

            migrationBuilder.DropIndex(
                name: "IX_Employes_ApplicationUserId",
                table: "Employes");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Employes");
        }
    }
}
