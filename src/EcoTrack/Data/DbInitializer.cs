using EcoTrack.Models;
using Microsoft.AspNetCore.Identity;

namespace EcoTrack.Data
{
    public static class DbInitializer
    {
        public const string RoleAdminPrincipal = "AdminPrincipal";
        public const string RoleAdminSecondaire = "AdminSecondaire";

        public static async Task SeedAsync(
            IServiceProvider services,
            IConfiguration configuration)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Création des rôles
            foreach (var role in new[]
            {
                RoleAdminPrincipal,
                RoleAdminSecondaire
            })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }


            // Création des administrateurs depuis appsettings.json
            var admins = configuration
                .GetSection("SeedAdmins")
                .GetChildren();


            foreach (var seedAdmin in admins)
            {
                var nomUtilisateur = seedAdmin["NomUtilisateur"];
                var email = seedAdmin["Email"];


                if (await userManager.FindByNameAsync(nomUtilisateur) == null)
                {
                    var admin = new ApplicationUser
                    {
                        UserName = nomUtilisateur,
                        Email = email,

                        Nom = seedAdmin["Nom"],
                        Prenom = seedAdmin["Prenom"],

                        EmailConfirmed = true,

                        // Le mot de passe est directement utilisable
                        DoitChangerMotDePasse = false
                    };


                    var motDePasse = seedAdmin["MotDePasse"];


                    var resultat = await userManager.CreateAsync(
                        admin,
                        motDePasse
                    );


                    if (resultat.Succeeded)
                    {
                        await userManager.AddToRoleAsync(
                            admin,
                            RoleAdminPrincipal
                        );
                    }
                }
            }
        }
    }
}