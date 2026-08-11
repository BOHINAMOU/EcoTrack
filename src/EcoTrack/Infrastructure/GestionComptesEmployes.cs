using EcoTrack.Data;
using EcoTrack.Models;
using Microsoft.AspNetCore.Identity;
using System.Globalization;
using System.Text;

namespace EcoTrack.Infrastructure
{
    /// <summary>
    /// Logique partagée de création de compte de connexion pour un employé.
    /// Utilisée à la création d'un employé (EmployesController) et lors de la
    /// nomination d'un admin temporaire (UtilisateursController), au cas où
    /// l'employé choisi n'a pas encore de compte.
    /// </summary>
    public static class GestionComptesEmployes
    {
        public class ResultatCreationCompte
        {
            public bool Succes { get; set; }
            public string? MessageErreur { get; set; }
            public string? MotDePasseGenere { get; set; }
            public string? NomUtilisateurGenere { get; set; }
        }

        private static string RetirerAccents(string texte)
        {
            var normalise = texte.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (var c in normalise)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }
            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string NormaliserPourUsername(string texte)
        {
            var sansAccents = RetirerAccents(texte).ToLowerInvariant();
            return new string(sansAccents.Where(char.IsLetterOrDigit).ToArray());
        }

        /// <summary>
        /// Génère un username unique : 1re lettre du prénom + nom. Si déjà pris, 2e lettre + nom,
        /// puis 3e, etc. Si toutes les lettres du prénom sont épuisées, ajoute un numéro à la fin.
        /// </summary>
        public static async Task<string> GenererNomUtilisateurAsync(string prenom, string nom, UserManager<ApplicationUser> userManager)
        {
            var prenomNormalise = NormaliserPourUsername(prenom);
            var nomNormalise = NormaliserPourUsername(nom);

            if (string.IsNullOrEmpty(prenomNormalise)) prenomNormalise = "x";
            if (string.IsNullOrEmpty(nomNormalise)) nomNormalise = "x";

            for (var i = 0; i < prenomNormalise.Length; i++)
            {
                var candidat = prenomNormalise[i] + nomNormalise;
                if (await userManager.FindByNameAsync(candidat) is null)
                {
                    return candidat;
                }
            }

            var baseCandidat = prenomNormalise[0] + nomNormalise;
            var compteur = 2;
            string candidatNumerote;
            do
            {
                candidatNumerote = baseCandidat + compteur;
                compteur++;
            } while (await userManager.FindByNameAsync(candidatNumerote) is not null);

            return candidatNumerote;
        }

        /// <summary>
        /// Crée un compte pour l'employé s'il n'en a pas déjà un, lui attribue le rôle "Employe",
        /// et envoie l'email d'identifiants. Si l'employé a déjà un compte (ApplicationUserId renseigné),
        /// ne fait rien et renvoie Succes = true.
        /// </summary>
        public static async Task<ResultatCreationCompte> CreerCompteSiAbsentAsync(
            Employe employe,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            string? nomUtilisateurSouhaite = null)
        {
            if (!string.IsNullOrEmpty(employe.ApplicationUserId))
            {
                return new ResultatCreationCompte { Succes = true };
            }

            var compteExistant = await userManager.FindByEmailAsync(employe.Email);
            if (compteExistant is not null)
            {
                employe.ApplicationUserId = compteExistant.Id;
                await context.SaveChangesAsync();

                if (!await userManager.IsInRoleAsync(compteExistant, DbInitializer.RoleEmploye)
                    && !await userManager.IsInRoleAsync(compteExistant, DbInitializer.RoleAdminPrincipal))
                {
                    await userManager.AddToRoleAsync(compteExistant, DbInitializer.RoleEmploye);
                }

                return new ResultatCreationCompte { Succes = true, NomUtilisateurGenere = compteExistant.UserName };
            }

            var motDePasseGenere = GenerateurMotDePasse.Generer();

            string nomUtilisateurGenere;
            var souhaiteNormalise = string.IsNullOrWhiteSpace(nomUtilisateurSouhaite) ? null : nomUtilisateurSouhaite.Trim();

            if (souhaiteNormalise is not null && await userManager.FindByNameAsync(souhaiteNormalise) is null)
            {
                nomUtilisateurGenere = souhaiteNormalise;
            }
            else
            {
                nomUtilisateurGenere = await GenererNomUtilisateurAsync(employe.Prenom, employe.Nom, userManager);
            }

            var nouvelUtilisateur = new ApplicationUser
            {
                UserName = nomUtilisateurGenere,
                Email = employe.Email,
                Nom = employe.Nom,
                Prenom = employe.Prenom,
                EmailConfirmed = true,
                DoitChangerMotDePasse = true
            };

            var resultatCreation = await userManager.CreateAsync(nouvelUtilisateur, motDePasseGenere);

            if (!resultatCreation.Succeeded)
            {
                return new ResultatCreationCompte
                {
                    Succes = false,
                    MessageErreur = string.Join(" ", resultatCreation.Errors.Select(e => e.Description))
                };
            }

            await userManager.AddToRoleAsync(nouvelUtilisateur, DbInitializer.RoleEmploye);

            employe.ApplicationUserId = nouvelUtilisateur.Id;
            await context.SaveChangesAsync();

            var corpsEmail = $@"
                <p>Bonjour {employe.Prenom} {employe.Nom},</p>
                <p>Un compte EcoTrack a été créé pour vous afin de consulter les actifs qui vous sont attribués.</p>
                <p><strong>Nom d'utilisateur :</strong> {nomUtilisateurGenere}<br/>
                <strong>Mot de passe temporaire :</strong> {motDePasseGenere}</p>
                <p>Pour des raisons de sécurité, il vous sera demandé de changer ce mot de passe dès votre première connexion. Vous pourrez aussi modifier votre nom d'utilisateur depuis votre profil.</p>
                <p>— EcoTrack, Ecobank Togo</p>";

            try
            {
                await emailSender.EnvoyerAsync(employe.Email, "Votre compte EcoTrack a été créé", corpsEmail);
            }
            catch (Exception)
            {
                return new ResultatCreationCompte
                {
                    Succes = true,
                    MotDePasseGenere = motDePasseGenere,
                    NomUtilisateurGenere = nomUtilisateurGenere,
                    MessageErreur = "email_non_envoye"
                };
            }

            return new ResultatCreationCompte { Succes = true, NomUtilisateurGenere = nomUtilisateurGenere };
        }
    }
}