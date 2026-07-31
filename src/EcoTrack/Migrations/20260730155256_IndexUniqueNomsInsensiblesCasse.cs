using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoTrack.Migrations
{
    /// <inheritdoc />
    public partial class IndexUniqueNomsInsensiblesCasse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX ""IX_Departements_Nom_CaseInsensitive""
                  ON ""Departements"" (LOWER(""Nom""));");

            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX ""IX_CategoriesActifs_Nom_CaseInsensitive""
                  ON ""CategoriesActifs"" (LOWER(""Nom""));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DROP INDEX IF EXISTS ""IX_Departements_Nom_CaseInsensitive"";");

            migrationBuilder.Sql(
                @"DROP INDEX IF EXISTS ""IX_CategoriesActifs_Nom_CaseInsensitive"";");
        }
    }
}