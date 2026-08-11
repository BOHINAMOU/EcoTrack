using EcoTrack.Models;
using Microsoft.AspNetCore.Identity;

namespace EcoTrack.Data
{
    public static class DbInitializer
    {
        public const string RoleAdminPrincipal = "AdminPrincipal";
        public const string RoleAdminTemporaire = "AdminTemporaire";
        public const string RoleEmploye = "Employe";

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
                RoleAdminTemporaire,
                RoleEmploye,
            })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Lecture des administrateurs dans appsettings.json
            var admins = configuration.GetSection("SeedAdmins").GetChildren();

            foreach (var seedAdmin in admins)
            {
                var nomUtilisateur = seedAdmin["NomUtilisateur"];
                var email = seedAdmin["Email"];
                var nom = seedAdmin["Nom"];
                var prenom = seedAdmin["Prenom"];
                var motDePasse = seedAdmin["MotDePasse"];

                // Vérification des valeurs obligatoires
                if (string.IsNullOrWhiteSpace(nomUtilisateur) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(motDePasse))
                {
                    continue;
                }

                // Vérifie si l'utilisateur existe déjà par nom d'utilisateur
                var utilisateur = await userManager.FindByNameAsync(nomUtilisateur);

                // Sinon vérifie par email
                utilisateur ??= await userManager.FindByEmailAsync(email);

                if (utilisateur == null)
                {
                    utilisateur = new ApplicationUser
                    {
                        UserName = nomUtilisateur,
                        Email = email,
                        Nom = nom ?? string.Empty,
                        Prenom = prenom ?? string.Empty,
                        EmailConfirmed = true,
                        DoitChangerMotDePasse = false
                    };

                    var resultat = await userManager.CreateAsync(utilisateur, motDePasse);

                    if (!resultat.Succeeded)
                    {
                        foreach (var erreur in resultat.Errors)
                        {
                            Console.WriteLine($"Erreur création utilisateur : {erreur.Description}");
                        }

                        continue;
                    }
                }

                // Attribue le rôle AdminPrincipal s'il ne l'a pas déjà
                if (!await userManager.IsInRoleAsync(utilisateur, RoleAdminPrincipal))
                {
                    await userManager.AddToRoleAsync(utilisateur, RoleAdminPrincipal);
                }
            }
        }
    }
}