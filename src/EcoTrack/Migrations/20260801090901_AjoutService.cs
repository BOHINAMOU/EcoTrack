using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcoTrack.Migrations
{
    /// <inheritdoc />
    public partial class AjoutService : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Créer la table Services
            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EstActif = table.Column<bool>(type: "boolean", nullable: false),
                    DepartementId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Services_Departements_DepartementId",
                        column: x => x.DepartementId,
                        principalTable: "Departements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Services_DepartementId",
                table: "Services",
                column: "DepartementId");

            // 2. Ajouter ServiceId sur Employes EN NULLABLE d'abord (pour ne pas casser les lignes existantes)
            migrationBuilder.AddColumn<int>(
                name: "ServiceId",
                table: "Employes",
                type: "integer",
                nullable: true);

            // 3. Créer un service "Général" pour chaque département existant
            migrationBuilder.Sql(@"
                INSERT INTO ""Services"" (""Nom"", ""EstActif"", ""DepartementId"")
                SELECT 'Général', true, ""Id"" FROM ""Departements"";
            ");

            // 4. Assigner chaque employé existant au service ""Général"" de son propre département
            migrationBuilder.Sql(@"
                UPDATE ""Employes"" e
                SET ""ServiceId"" = s.""Id""
                FROM ""Services"" s
                WHERE s.""DepartementId"" = e.""DepartementId"" AND s.""Nom"" = 'Général';
            ");

            // 5. Rendre la colonne obligatoire maintenant que toutes les lignes sont remplies
            migrationBuilder.AlterColumn<int>(
                name: "ServiceId",
                table: "Employes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employes_ServiceId",
                table: "Employes",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employes_Services_ServiceId",
                table: "Employes",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employes_Services_ServiceId",
                table: "Employes");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Employes_ServiceId",
                table: "Employes");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "Employes");
        }
    }
}
