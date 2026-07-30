using EcoTrack.Models;
using Microsoft.AspNetCore.Identity;

namespace EcoTrack.Data
{
    public static class DbInitializer
    {
        public const string RoleAdminPrincipal = "AdminPrincipal";
        public const string RoleAdminSecondaire = "AdminSecondaire";

        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var role in new[] { RoleAdminPrincipal, RoleAdminSecondaire })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var seedSection = configuration.GetSection("SeedAdmin");
            var nomUtilisateur = seedSection["NomUtilisateur"] ?? "admin.principal";
            var email = seedSection["Email"] ?? "admin@ecobank.tg";

            if (await userManager.FindByNameAsync(nomUtilisateur) is null)
            {
                var admin = new ApplicationUser
                {
                    UserName = nomUtilisateur,
                    Email = email,
                    Nom = seedSection["Nom"] ?? "Administrateur",
                    Prenom = seedSection["Prenom"] ?? "Principal",
                    EmailConfirmed = true,
                    DoitChangerMotDePasse = true
                };

                var motDePasse = seedSection["MotDePasse"] ?? "Ecobank@2026";
                var resultat = await userManager.CreateAsync(admin, motDePasse);

                if (resultat.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, RoleAdminPrincipal);
                }
            }
        }
    }
}