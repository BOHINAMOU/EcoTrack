using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcoTrack.Migrations
{
    /// <inheritdoc />
    public partial class MiseAJourStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actifs_Departements_DepartementId",
                table: "Actifs");

            migrationBuilder.DropForeignKey(
                name: "FK_Affectations_Actifs_ActifId",
                table: "Affectations");

            migrationBuilder.DropForeignKey(
                name: "FK_Employes_Departements_DepartementId",
                table: "Employes");

            migrationBuilder.DropForeignKey(
                name: "FK_Employes_Services_ServiceId",
                table: "Employes");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalActions_AspNetUsers_UtilisateurId",
                table: "JournalActions");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Departements_DepartementId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Actifs_NumeroSerie",
                table: "Actifs");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Departements");

            migrationBuilder.DropColumn(
                name: "Localisation",
                table: "Departements");

            migrationBuilder.RenameColumn(
                name: "DepartementId",
                table: "Services",
                newName: "DivisionId");

            migrationBuilder.RenameIndex(
                name: "IX_Services_DepartementId",
                table: "Services",
                newName: "IX_Services_DivisionId");

            migrationBuilder.RenameColumn(
                name: "DepartementId",
                table: "Employes",
                newName: "UniteId");

            migrationBuilder.RenameIndex(
                name: "IX_Employes_DepartementId",
                table: "Employes",
                newName: "IX_Employes_UniteId");

            migrationBuilder.RenameColumn(
                name: "DepartementId",
                table: "Actifs",
                newName: "AgenceId");

            migrationBuilder.RenameIndex(
                name: "IX_Actifs_DepartementId",
                table: "Actifs",
                newName: "IX_Actifs_AgenceId");

            migrationBuilder.AddColumn<int>(
                name: "AgenceId",
                table: "Departements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Prenom",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Nom",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "DoitChangerMotDePasse",
                table: "AspNetUsers",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateCreation",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "AspNetUsers",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Agences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nom = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Localisation = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    EstActif = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Divisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nom = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EstActif = table.Column<bool>(type: "boolean", nullable: false),
                    DepartementId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Divisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Divisions_Departements_DepartementId",
                        column: x => x.DepartementId,
                        principalTable: "Departements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Unites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nom = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EstActif = table.Column<bool>(type: "boolean", nullable: false),
                    ServiceId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Unites_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Departements_AgenceId",
                table: "Departements",
                column: "AgenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_DepartementId",
                table: "Divisions",
                column: "DepartementId");

            migrationBuilder.CreateIndex(
                name: "IX_Unites_ServiceId",
                table: "Unites",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Actifs_Agences_AgenceId",
                table: "Actifs",
                column: "AgenceId",
                principalTable: "Agences",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Affectations_Actifs_ActifId",
                table: "Affectations",
                column: "ActifId",
                principalTable: "Actifs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departements_Agences_AgenceId",
                table: "Departements",
                column: "AgenceId",
                principalTable: "Agences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employes_Services_ServiceId",
                table: "Employes",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Employes_Unites_UniteId",
                table: "Employes",
                column: "UniteId",
                principalTable: "Unites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalActions_AspNetUsers_UtilisateurId",
                table: "JournalActions",
                column: "UtilisateurId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Divisions_DivisionId",
                table: "Services",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actifs_Agences_AgenceId",
                table: "Actifs");

            migrationBuilder.DropForeignKey(
                name: "FK_Affectations_Actifs_ActifId",
                table: "Affectations");

            migrationBuilder.DropForeignKey(
                name: "FK_Departements_Agences_AgenceId",
                table: "Departements");

            migrationBuilder.DropForeignKey(
                name: "FK_Employes_Services_ServiceId",
                table: "Employes");

            migrationBuilder.DropForeignKey(
                name: "FK_Employes_Unites_UniteId",
                table: "Employes");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalActions_AspNetUsers_UtilisateurId",
                table: "JournalActions");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Divisions_DivisionId",
                table: "Services");

            migrationBuilder.DropTable(
                name: "Agences");

            migrationBuilder.DropTable(
                name: "Divisions");

            migrationBuilder.DropTable(
                name: "Unites");

            migrationBuilder.DropIndex(
                name: "IX_Departements_AgenceId",
                table: "Departements");

            migrationBuilder.DropColumn(
                name: "AgenceId",
                table: "Departements");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "DivisionId",
                table: "Services",
                newName: "DepartementId");

            migrationBuilder.RenameIndex(
                name: "IX_Services_DivisionId",
                table: "Services",
                newName: "IX_Services_DepartementId");

            migrationBuilder.RenameColumn(
                name: "UniteId",
                table: "Employes",
                newName: "DepartementId");

            migrationBuilder.RenameIndex(
                name: "IX_Employes_UniteId",
                table: "Employes",
                newName: "IX_Employes_DepartementId");

            migrationBuilder.RenameColumn(
                name: "AgenceId",
                table: "Actifs",
                newName: "DepartementId");

            migrationBuilder.RenameIndex(
                name: "IX_Actifs_AgenceId",
                table: "Actifs",
                newName: "IX_Actifs_DepartementId");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Departements",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Localisation",
                table: "Departements",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Prenom",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nom",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "DoitChangerMotDePasse",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateCreation",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Actifs_NumeroSerie",
                table: "Actifs",
                column: "NumeroSerie",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Actifs_Departements_DepartementId",
                table: "Actifs",
                column: "DepartementId",
                principalTable: "Departements",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Affectations_Actifs_ActifId",
                table: "Affectations",
                column: "ActifId",
                principalTable: "Actifs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Employes_Departements_DepartementId",
                table: "Employes",
                column: "DepartementId",
                principalTable: "Departements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employes_Services_ServiceId",
                table: "Employes",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalActions_AspNetUsers_UtilisateurId",
                table: "JournalActions",
                column: "UtilisateurId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Departements_DepartementId",
                table: "Services",
                column: "DepartementId",
                principalTable: "Departements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
