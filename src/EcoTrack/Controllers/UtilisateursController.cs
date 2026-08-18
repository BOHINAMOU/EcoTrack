using EcoTrack.Data;
using EcoTrack.Infrastructure;
using EcoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using IEmailSender = EcoTrack.Infrastructure.IEmailSender;

namespace EcoTrack.Controllers
{
    [Authorize(Roles = "AdminPrincipal")]
    public class UtilisateursController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;
        private readonly IJournalService _journal;

        public UtilisateursController(UserManager<ApplicationUser> userManager, IEmailSender emailSender, ApplicationDbContext context, IJournalService journal)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _context = context;
            _journal = journal;
        }

        private string UtilisateurConnecteId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        // GET /Utilisateurs — comptes ayant un rôle d'administration (principal ou temporaire)
        public async Task<IActionResult> Index()
        {
            var adminsTemporaires = await _userManager.GetUsersInRoleAsync(DbInitializer.RoleAdminTemporaire);

            var idsLiesAUnEmploye = (await _context.Employes
                .Where(e => e.ApplicationUserId != null)
                .Select(e => e.ApplicationUserId!)
                .ToListAsync())
                .ToHashSet();

            var viewModel = adminsTemporaires
                .Select(u => (Utilisateur: u, EstLieAUnEmploye: idsLiesAUnEmploye.Contains(u.Id)))
                .OrderBy(v => v.Utilisateur.Nom)
                .ToList();

            return View(viewModel);
        }

        // GET /Utilisateurs/TousLesUtilisateurs?terme=...
        // Vue d'ensemble de TOUS les comptes employés (pas seulement les admins temporaires) :
        // modifier le profil, réinitialiser le mot de passe, activer/désactiver.
        public async Task<IActionResult> TousLesUtilisateurs(string? terme)
        {
            var requete = _context.Employes.Where(e => e.ApplicationUserId != null).AsQueryable();

            if (!string.IsNullOrWhiteSpace(terme))
            {
                var termeNormalise = terme.Trim().ToLower();
                requete = requete.Where(e =>
                    (e.Nom + " " + e.Prenom).ToLower().Contains(termeNormalise) ||
                    (e.Prenom + " " + e.Nom).ToLower().Contains(termeNormalise) ||
                    e.Email.ToLower().Contains(termeNormalise));
            }

            var employes = await requete.OrderBy(e => e.Nom).ToListAsync();
            var idsComptes = employes.Select(e => e.ApplicationUserId!).ToList();

            var comptes = await _userManager.Users
                .Where(u => idsComptes.Contains(u.Id))
                .ToListAsync();
            var comptesParId = comptes.ToDictionary(u => u.Id);

            var viewModel = employes
                .Where(e => comptesParId.ContainsKey(e.ApplicationUserId!))
                .Select(e => (Employe: e, Compte: comptesParId[e.ApplicationUserId!]))
                .ToList();

            ViewBag.Terme = terme;
            return View(viewModel);
        }

        // GET /Utilisateurs/ModifierEmploye/{id} (id = Employe.Id)
        public async Task<IActionResult> ModifierEmploye(int id)
        {
            var employe = await _context.Employes.FirstOrDefaultAsync(e => e.Id == id);
            if (employe is null || string.IsNullOrEmpty(employe.ApplicationUserId))
            {
                return NotFound();
            }

            var compte = await _userManager.FindByIdAsync(employe.ApplicationUserId);
            if (compte is null)
            {
                return NotFound();
            }

            var (indicatif, numero) = DecomposerTelephone(employe.Telephone);

            var viewModel = new EcoTrack.ViewModels.ModifierEmployeCompteViewModel
            {
                Nom = employe.Nom,
                Prenom = employe.Prenom,
                Email = employe.Email,
                NomUtilisateur = compte.UserName ?? string.Empty,
                Poste = employe.Poste,
                Indicatif = indicatif,
                NumeroTelephone = numero
            };

            ViewBag.EmployeId = id;
            return View(viewModel);
        }

        // POST /Utilisateurs/ModifierEmploye/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ModifierEmploye(int id, EcoTrack.ViewModels.ModifierEmployeCompteViewModel model)
        {
            var employe = await _context.Employes.FirstOrDefaultAsync(e => e.Id == id);
            if (employe is null || string.IsNullOrEmpty(employe.ApplicationUserId))
            {
                return NotFound();
            }

            var compte = await _userManager.FindByIdAsync(employe.ApplicationUserId);
            if (compte is null)
            {
                return NotFound();
            }

            var telephoneComplet = $"{model.Indicatif} {model.NumeroTelephone}".Trim();
            var emailNormalise = model.Email.Trim().ToLower();
            var nomUtilisateurNormalise = model.NomUtilisateur.Trim().ToLower();

            if (await _context.Employes.AnyAsync(e => e.Id != id && e.Email.ToLower() == emailNormalise))
            {
                ModelState.AddModelError(nameof(model.Email), "Cet email est déjà utilisé par un autre employé.");
            }

            if (await _context.Employes.AnyAsync(e => e.Id != id && e.Telephone == telephoneComplet))
            {
                ModelState.AddModelError(nameof(model.NumeroTelephone), "Ce numéro de téléphone est déjà utilisé par un autre employé.");
            }

            var conflitNomUtilisateur = await _userManager.FindByNameAsync(nomUtilisateurNormalise);
            if (conflitNomUtilisateur is not null && conflitNomUtilisateur.Id != compte.Id)
            {
                ModelState.AddModelError(nameof(model.NomUtilisateur), "Ce nom d'utilisateur est déjà pris.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.EmployeId = id;
                return View(model);
            }

            employe.Nom = model.Nom;
            employe.Prenom = model.Prenom;
            employe.Email = model.Email;
            employe.Poste = model.Poste;
            employe.Telephone = telephoneComplet;
            await _context.SaveChangesAsync();

            compte.Nom = model.Nom;
            compte.Prenom = model.Prenom;
            compte.Email = model.Email;
            compte.UserName = model.NomUtilisateur;
            await _userManager.UpdateAsync(compte);

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "ModificationEmployeParAdmin",
                $"A modifié les informations de \"{model.Prenom} {model.Nom}\" depuis la gestion des utilisateurs.");

            TempData["Succes"] = $"Les informations de \"{model.Prenom} {model.Nom}\" ont été mises à jour.";
            return RedirectToAction(nameof(TousLesUtilisateurs));
        }

        private static (string Indicatif, string Numero) DecomposerTelephone(string? telephone)
        {
            if (string.IsNullOrWhiteSpace(telephone))
            {
                return ("+228", string.Empty);
            }

            var parties = telephone.Split(' ', 2);
            return parties.Length == 2 ? (parties[0], parties[1]) : ("+228", telephone);
        }

        // GET /Utilisateurs/ModifierProfil/{id}
        public async Task<IActionResult> ModifierProfil(string id, string? retour)
        {
            var utilisateur = await _userManager.FindByIdAsync(id);
            if (utilisateur is null)
            {
                return NotFound();
            }

            if (await _userManager.IsInRoleAsync(utilisateur, DbInitializer.RoleAdminPrincipal))
            {
                return Forbid();
            }

            var viewModel = new EcoTrack.ViewModels.ProfilViewModel
            {
                Nom = utilisateur.Nom,
                Prenom = utilisateur.Prenom,
                NomUtilisateur = utilisateur.UserName ?? string.Empty,
                Email = utilisateur.Email ?? string.Empty
            };

            ViewBag.UtilisateurId = id;
            ViewBag.Retour = retour;
            return View(viewModel);
        }

        // POST /Utilisateurs/ModifierProfil/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ModifierProfil(string id, EcoTrack.ViewModels.ProfilViewModel model, string? retour)
        {
            var utilisateur = await _userManager.FindByIdAsync(id);
            if (utilisateur is null)
            {
                return NotFound();
            }

            if (await _userManager.IsInRoleAsync(utilisateur, DbInitializer.RoleAdminPrincipal))
            {
                return Forbid();
            }

            var nomUtilisateurNormalise = model.NomUtilisateur.Trim().ToLower();
            var emailNormalise = model.Email.Trim().ToLower();

            var conflitNomUtilisateur = await _userManager.FindByNameAsync(nomUtilisateurNormalise);
            if (conflitNomUtilisateur is not null && conflitNomUtilisateur.Id != utilisateur.Id)
            {
                ModelState.AddModelError(nameof(model.NomUtilisateur), "Ce nom d'utilisateur est déjà pris.");
            }

            var conflitEmail = await _userManager.FindByEmailAsync(emailNormalise);
            if (conflitEmail is not null && conflitEmail.Id != utilisateur.Id)
            {
                ModelState.AddModelError(nameof(model.Email), "Cet email est déjà utilisé par un autre compte.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.UtilisateurId = id;
                ViewBag.Retour = retour;
                return View(model);
            }

            utilisateur.Nom = model.Nom;
            utilisateur.Prenom = model.Prenom;
            utilisateur.UserName = model.NomUtilisateur;
            utilisateur.Email = model.Email;
            await _userManager.UpdateAsync(utilisateur);

            // Si ce compte est lié à un employé, on garde le nom/prénom/email cohérents des deux côtés.
            var employeLie = await _context.Employes.FirstOrDefaultAsync(e => e.ApplicationUserId == utilisateur.Id);
            if (employeLie is not null)
            {
                employeLie.Nom = model.Nom;
                employeLie.Prenom = model.Prenom;
                employeLie.Email = model.Email;
                await _context.SaveChangesAsync();
            }

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "ModificationProfilAdmin",
                $"A modifié le profil de \"{model.Prenom} {model.Nom}\".");

            TempData["Succes"] = "Le profil a été mis à jour.";
            return RedirectToAction(retour ?? nameof(Index));
        }

        // GET /Utilisateurs/Actions?utilisateurId=...
        public async Task<IActionResult> Actions(string? utilisateurId)
        {
            var requete = _context.JournalActions
                .Include(j => j.Utilisateur)
                .AsQueryable();

            if (!string.IsNullOrEmpty(utilisateurId))
            {
                requete = requete.Where(j => j.UtilisateurId == utilisateurId);
            }

            var actions = await requete
                .OrderByDescending(j => j.DateAction)
                .Take(200)
                .ToListAsync();

            ViewBag.Utilisateurs = _userManager.Users.OrderBy(u => u.Nom).ToList();
            ViewBag.UtilisateurIdFiltre = utilisateurId;

            return View(actions);
        }

        // GET /Utilisateurs/ActionsPdf?utilisateurId=...
        // Meme filtre que la page Actions : vide = tout le monde, sinon un utilisateur precis.
        public async Task<IActionResult> ActionsPdf(string? utilisateurId)
        {
            var requete = _context.JournalActions
                .Include(j => j.Utilisateur)
                .AsQueryable();

            if (!string.IsNullOrEmpty(utilisateurId))
            {
                requete = requete.Where(j => j.UtilisateurId == utilisateurId);
            }

            var actions = await requete
                .OrderByDescending(j => j.DateAction)
                .Take(500)
                .ToListAsync();

            string? nomUtilisateurFiltre = null;
            if (!string.IsNullOrEmpty(utilisateurId))
            {
                var u = await _userManager.FindByIdAsync(utilisateurId);
                nomUtilisateurFiltre = u is not null ? $"{u.Prenom} {u.Nom}" : null;
            }

            var document = new RapportActionsDocument(actions, nomUtilisateurFiltre);
            var pdf = document.GeneratePdf();

            var nomFichier = nomUtilisateurFiltre is not null
                ? $"Actions_{nomUtilisateurFiltre.Replace(" ", "_")}.pdf"
                : "Actions_Tous_Utilisateurs.pdf";

            return File(pdf, "application/pdf", nomFichier);
        }

        // GET /Utilisateurs/CreerTemporaire
        // Compte autonome (pas lié à un employé), accès complet, avec ou sans date d'expiration.
        // Utile pour un contrôleur/auditeur externe qui a besoin d'un accès ponctuel.
        [HttpGet]
        public IActionResult CreerTemporaire()
        {
            return View(new EcoTrack.ViewModels.UtilisateurCreerViewModel());
        }

        // POST /Utilisateurs/CreerTemporaire
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreerTemporaire(EcoTrack.ViewModels.UtilisateurCreerViewModel model)
        {
            var nomUtilisateurNormalise = (model.NomUtilisateur ?? string.Empty).Trim().ToLower();
            var emailNormalise = (model.Email ?? string.Empty).Trim().ToLower();

            if (await _userManager.FindByNameAsync(nomUtilisateurNormalise) is not null)
            {
                ModelState.AddModelError(nameof(model.NomUtilisateur), "Ce nom d'utilisateur est déjà utilisé.");
            }

            if (await _userManager.FindByEmailAsync(emailNormalise) is not null)
            {
                ModelState.AddModelError(nameof(model.Email), "Cet email est déjà utilisé par un autre compte.");
            }

            if (model.DateExpiration is not null && model.DateExpiration.Value.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(nameof(model.DateExpiration), "La date d'expiration ne peut pas être dans le passé.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var motDePasseGenere = GenerateurMotDePasse.Generer();

            var nouvelUtilisateur = new ApplicationUser
            {
                UserName = model.NomUtilisateur,
                Email = model.Email,
                Nom = model.Nom,
                Prenom = model.Prenom,
                EmailConfirmed = true,
                DoitChangerMotDePasse = true,
                CreeParId = UtilisateurConnecteId,
                DateExpirationAcces = model.DateExpiration?.Date.AddDays(1).AddTicks(-1) // fin de journée incluse
            };

            var resultat = await _userManager.CreateAsync(nouvelUtilisateur, motDePasseGenere);

            if (!resultat.Succeeded)
            {
                foreach (var erreur in resultat.Errors)
                {
                    ModelState.AddModelError(string.Empty, erreur.Description);
                }
                return View(model);
            }

            await _userManager.AddToRoleAsync(nouvelUtilisateur, DbInitializer.RoleAdminTemporaire);

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "CreationCompteTemporaire",
                $"A créé le compte temporaire \"{model.Prenom} {model.Nom}\"" +
                (model.DateExpiration is not null ? $" (accès jusqu'au {model.DateExpiration.Value:dd/MM/yyyy})" : " (sans expiration automatique)"));

            var expirationTexte = model.DateExpiration is not null
                ? $"<p>Cet accès expirera automatiquement le <strong>{model.DateExpiration.Value:dd/MM/yyyy}</strong>.</p>"
                : "";

            var corpsEmail = $@"
                <p>Bonjour {model.Prenom} {model.Nom},</p>
                <p>Un compte EcoTrack avec accès complet à la plateforme a été créé pour vous.</p>
                <p><strong>Nom d'utilisateur :</strong> {model.NomUtilisateur}<br/>
                <strong>Mot de passe temporaire :</strong> {motDePasseGenere}</p>
                {expirationTexte}
                <p>Pour des raisons de sécurité, il vous sera demandé de changer ce mot de passe dès votre première connexion.</p>
                <p>— EcoTrack, Ecobank Togo</p>";

            try
            {
                await _emailSender.EnvoyerAsync(model.Email, "Votre compte EcoTrack a été créé", corpsEmail);
                TempData["Succes"] = $"Le compte \"{model.Prenom} {model.Nom}\" a été créé et un email lui a été envoyé avec ses identifiants.";
            }
            catch (Exception)
            {
                TempData["Erreur"] = $"Le compte a été créé, mais l'email n'a pas pu être envoyé. Mot de passe temporaire : {motDePasseGenere} (à communiquer manuellement).";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST /Utilisateurs/Supprimer/{id}
        // Suppression réelle et définitive du compte. Réservée aux comptes autonomes
        // (pas liés à un employé) pour ne jamais couper l'accès "Mon espace" d'un employé par erreur.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Supprimer(string id)
        {
            var utilisateur = await _userManager.FindByIdAsync(id);
            if (utilisateur is null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(utilisateur);
            if (roles.Contains(DbInitializer.RoleAdminPrincipal))
            {
                TempData["Erreur"] = "Impossible de supprimer le compte administrateur principal.";
                return RedirectToAction(nameof(Index));
            }

            var estLieAUnEmploye = await _context.Employes.AnyAsync(e => e.ApplicationUserId == utilisateur.Id);
            if (estLieAUnEmploye)
            {
                TempData["Erreur"] = "Ce compte est celui d'un employé — utilise \"Retirer les droits admin\" plutôt qu'une suppression, sinon il perdrait aussi l'accès à son espace employé.";
                return RedirectToAction(nameof(Index));
            }

            var nomComplet = $"{utilisateur.Prenom} {utilisateur.Nom}";
            await _userManager.DeleteAsync(utilisateur);

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "SuppressionCompte",
                $"A supprimé définitivement le compte \"{nomComplet}\".");

            TempData["Succes"] = $"Le compte \"{nomComplet}\" a été supprimé définitivement.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Utilisateurs/Nommer?terme=...
        // Choix d'un employé à nommer administrateur temporaire (droits identiques à l'admin principal).
        public async Task<IActionResult> Nommer(string? terme)
        {
            var idsDejaAdminTemporaire = (await _userManager.GetUsersInRoleAsync(DbInitializer.RoleAdminTemporaire))
                .Select(u => u.Id)
                .ToHashSet();

            var requete = _context.Employes
                .Where(e => e.EstActif)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(terme))
            {
                var termeNormalise = terme.Trim().ToLower();
                requete = requete.Where(e =>
                    (e.Nom + " " + e.Prenom).ToLower().Contains(termeNormalise) ||
                    (e.Prenom + " " + e.Nom).ToLower().Contains(termeNormalise) ||
                    e.Email.ToLower().Contains(termeNormalise));
            }

            var employes = await requete.OrderBy(e => e.Nom).ToListAsync();

            ViewBag.Terme = terme;
            ViewBag.IdsDejaAdminTemporaire = idsDejaAdminTemporaire;

            return View(employes);
        }

        // POST /Utilisateurs/Nommer/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Nommer(int employeId)
        {
            var employe = await _context.Employes.FirstOrDefaultAsync(e => e.Id == employeId);
            if (employe is null)
            {
                return NotFound();
            }

            var resultatCompte = await GestionComptesEmployes.CreerCompteSiAbsentAsync(employe, _context, _userManager, _emailSender);
            if (!resultatCompte.Succes)
            {
                TempData["Erreur"] = $"Impossible de créer un compte pour cet employé : {resultatCompte.MessageErreur}";
                return RedirectToAction(nameof(Nommer));
            }

            var utilisateur = await _userManager.FindByIdAsync(employe.ApplicationUserId!);
            if (utilisateur is null)
            {
                TempData["Erreur"] = "Le compte de l'employé est introuvable.";
                return RedirectToAction(nameof(Nommer));
            }

            if (!await _userManager.IsInRoleAsync(utilisateur, DbInitializer.RoleAdminTemporaire))
            {
                await _userManager.AddToRoleAsync(utilisateur, DbInitializer.RoleAdminTemporaire);
            }

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "NominationAdminTemporaire",
                $"A nommé \"{employe.Prenom} {employe.Nom}\" administrateur temporaire.");

            var corpsEmail = $@"
                <p>Bonjour {employe.Prenom} {employe.Nom},</p>
                <p>Vous avez été désigné(e) administrateur temporaire d'EcoTrack. Vous disposez désormais des mêmes droits que l'administrateur principal, en plus de votre espace employé habituel.</p>
                <p>Connectez-vous avec vos identifiants habituels (email : {employe.Email}).</p>
                <p>— EcoTrack, Ecobank Togo</p>";

            try
            {
                await _emailSender.EnvoyerAsync(employe.Email, "Vous êtes désormais administrateur temporaire EcoTrack", corpsEmail);
            }
            catch (Exception)
            {
                // L'échec d'envoi ne doit pas bloquer la nomination.
            }

            TempData["Succes"] = $"\"{employe.Prenom} {employe.Nom}\" a été nommé(e) administrateur temporaire.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Utilisateurs/Revoquer/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoquer(string id)
        {
            var utilisateur = await _userManager.FindByIdAsync(id);
            if (utilisateur is null)
            {
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(utilisateur, DbInitializer.RoleAdminTemporaire))
            {
                TempData["Erreur"] = "Ce compte n'est pas administrateur temporaire.";
                return RedirectToAction(nameof(Index));
            }

            await _userManager.RemoveFromRoleAsync(utilisateur, DbInitializer.RoleAdminTemporaire);

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "RevocationAdminTemporaire",
                $"A retiré les droits d'administrateur temporaire de \"{utilisateur.Prenom} {utilisateur.Nom}\".");

            TempData["Succes"] = $"Les droits d'administrateur temporaire de \"{utilisateur.Prenom} {utilisateur.Nom}\" ont été retirés.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Utilisateurs/ReinitialiserMotDePasse/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReinitialiserMotDePasse(string id, string? retour)
        {
            var utilisateur = await _userManager.FindByIdAsync(id);
            if (utilisateur is null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(utilisateur);
            if (roles.Contains(DbInitializer.RoleAdminPrincipal))
            {
                TempData["Erreur"] = "Impossible de réinitialiser le mot de passe de l'admin principal depuis cette page.";
                return RedirectToAction(retour ?? nameof(Index));
            }

            var nouveauMotDePasse = GenerateurMotDePasse.Generer();

            var jeton = await _userManager.GeneratePasswordResetTokenAsync(utilisateur);
            var resultat = await _userManager.ResetPasswordAsync(utilisateur, jeton, nouveauMotDePasse);

            if (!resultat.Succeeded)
            {
                TempData["Erreur"] = "La réinitialisation a échoué.";
                return RedirectToAction(retour ?? nameof(Index));
            }

            utilisateur.DoitChangerMotDePasse = true;
            await _userManager.UpdateAsync(utilisateur);

            var corpsEmail = $@"
                <p>Bonjour {utilisateur.Prenom} {utilisateur.Nom},</p>
                <p>Votre mot de passe EcoTrack a été réinitialisé par l'administrateur principal.</p>
                <p><strong>Nouveau mot de passe temporaire :</strong> {nouveauMotDePasse}</p>
                <p>Il vous sera demandé de le changer dès votre prochaine connexion.</p>
                <p>— EcoTrack, Ecobank Togo</p>";

            try
            {
                await _emailSender.EnvoyerAsync(utilisateur.Email!, "Réinitialisation de votre mot de passe EcoTrack", corpsEmail);
                TempData["Succes"] = $"Le mot de passe de \"{utilisateur.Prenom} {utilisateur.Nom}\" a été réinitialisé et envoyé par email.";
            }
            catch (Exception)
            {
                TempData["Erreur"] = $"Mot de passe réinitialisé, mais l'email n'a pas pu être envoyé. Nouveau mot de passe : {nouveauMotDePasse} (à communiquer manuellement).";
            }

            return RedirectToAction(retour ?? nameof(Index));
        }

        // POST /Utilisateurs/BasculerActivation/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BasculerActivation(string id, string? retour)
        {
            var utilisateur = await _userManager.FindByIdAsync(id);
            if (utilisateur is null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(utilisateur);
            if (roles.Contains(DbInitializer.RoleAdminPrincipal))
            {
                TempData["Erreur"] = "Impossible de désactiver le compte administrateur principal.";
                return RedirectToAction(retour ?? nameof(Index));
            }

            var estActuellementVerrouille = await _userManager.IsLockedOutAsync(utilisateur);

            if (estActuellementVerrouille)
            {
                await _userManager.SetLockoutEndDateAsync(utilisateur, null);
                TempData["Succes"] = $"\"{utilisateur.Prenom} {utilisateur.Nom}\" a été réactivé.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(utilisateur, DateTimeOffset.MaxValue);
                TempData["Succes"] = $"\"{utilisateur.Prenom} {utilisateur.Nom}\" a été désactivé.";
            }

            return RedirectToAction(retour ?? nameof(Index));
        }
    }

    public class RapportActionsDocument : IDocument
    {
        private readonly List<JournalAction> _actions;
        private readonly string? _nomUtilisateurFiltre;

        public RapportActionsDocument(List<JournalAction> actions, string? nomUtilisateurFiltre)
        {
            _actions = actions;
            _nomUtilisateurFiltre = nomUtilisateurFiltre;
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Background("#0d3b66").Padding(15).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("HISTORIQUE DES ACTIONS").FontSize(16).Bold().FontColor(Colors.White);
                        col.Item().Text("EcoTrack — Ecobank Togo").FontSize(9).FontColor(Colors.Grey.Lighten3);
                    });
                    row.ConstantItem(180).AlignRight().Column(col =>
                    {
                        col.Item().Text(_nomUtilisateurFiltre ?? "Tous les utilisateurs").FontSize(9).FontColor(Colors.Grey.Lighten3);
                        col.Item().Text($"{_actions.Count} action(s)").FontSize(9).FontColor(Colors.Grey.Lighten3);
                    });
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(110);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(5);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background("#0d3b66").Padding(4).Text("Date").FontColor(Colors.White).Bold();
                            header.Cell().Background("#0d3b66").Padding(4).Text("Utilisateur").FontColor(Colors.White).Bold();
                            header.Cell().Background("#0d3b66").Padding(4).Text("Type").FontColor(Colors.White).Bold();
                            header.Cell().Background("#0d3b66").Padding(4).Text("Description").FontColor(Colors.White).Bold();
                        });

                        foreach (var action in _actions)
                        {
                            table.Cell().Padding(4).Text(action.DateAction.ToString("dd/MM/yyyy HH:mm"));
                            table.Cell().Padding(4).Text($"{action.Utilisateur?.Prenom} {action.Utilisateur?.Nom}");
                            table.Cell().Padding(4).Text(action.TypeAction);
                            table.Cell().Padding(4).Text(action.Description);
                        }
                    });

                    if (!_actions.Any())
                    {
                        col.Item().PaddingTop(10).Text("Aucune action enregistrée pour ce filtre.").Italic().FontColor(Colors.Grey.Darken1);
                    }
                });

                page.Footer().Padding(10).Row(row =>
                {
                    row.RelativeItem().Text("EcoTrack — Système de gestion des actifs Ecobank Togo").FontSize(8).FontColor(Colors.Grey.Darken1);
                    row.RelativeItem().AlignRight().Text(x =>
                    {
                        x.Span("Généré le ").FontSize(8).FontColor(Colors.Grey.Darken1);
                        x.Span(DateTime.UtcNow.ToString("dd/MM/yyyy à HH:mm")).FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });
            });
        }
    }
}