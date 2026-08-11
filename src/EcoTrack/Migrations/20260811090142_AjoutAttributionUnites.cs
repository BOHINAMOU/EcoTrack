using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoTrack.Migrations
{
    /// <inheritdoc />
    public partial class AjoutAttributionUnites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartementId",
                table: "Actifs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DivisionId",
                table: "Actifs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceId",
                table: "Actifs",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Actifs_DepartementId",
                table: "Actifs",
                column: "DepartementId");

            migrationBuilder.CreateIndex(
                name: "IX_Actifs_DivisionId",
                table: "Actifs",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Actifs_ServiceId",
                table: "Actifs",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Actifs_Departements_DepartementId",
                table: "Actifs",
                column: "DepartementId",
                principalTable: "Departements",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Actifs_Divisions_DivisionId",
                table: "Actifs",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Actifs_Services_ServiceId",
                table: "Actifs",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actifs_Departements_DepartementId",
                table: "Actifs");

            migrationBuilder.DropForeignKey(
                name: "FK_Actifs_Divisions_DivisionId",
                table: "Actifs");

            migrationBuilder.DropForeignKey(
                name: "FK_Actifs_Services_ServiceId",
                table: "Actifs");

            migrationBuilder.DropIndex(
                name: "IX_Actifs_DepartementId",
                table: "Actifs");

            migrationBuilder.DropIndex(
                name: "IX_Actifs_DivisionId",
                table: "Actifs");

            migrationBuilder.DropIndex(
                name: "IX_Actifs_ServiceId",
                table: "Actifs");

            migrationBuilder.DropColumn(
                name: "DepartementId",
                table: "Actifs");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "Actifs");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "Actifs");
        }
    }
}
