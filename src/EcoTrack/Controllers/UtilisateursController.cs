using EcoTrack.Data;
using EcoTrack.Infrastructure;
using EcoTrack.Models;
using EcoTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IEmailSender = EcoTrack.Infrastructure.IEmailSender;

namespace EcoTrack.Controllers
{
    [Authorize(Roles = "AdminPrincipal")]
    public class UtilisateursController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;

        public UtilisateursController(UserManager<ApplicationUser> userManager, IEmailSender emailSender, ApplicationDbContext context)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _context = context;
        }

        // GET /Utilisateurs
        public async Task<IActionResult> Index()
        {
            var utilisateurs = _userManager.Users.OrderBy(u => u.Nom).ToList();

            var viewModel = new List<(ApplicationUser Utilisateur, string Role)>();
            foreach (var utilisateur in utilisateurs)
            {
                var roles = await _userManager.GetRolesAsync(utilisateur);
                viewModel.Add((utilisateur, roles.FirstOrDefault() ?? "Aucun rôle"));
            }

            return View(viewModel);
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

        // GET /Utilisateurs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST /Utilisateurs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UtilisateurCreerViewModel model)
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

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var motDePasseGenere = GenerateurMotDePasse.Generer();
            var adminPrincipalId = _userManager.GetUserId(User);

            var nouvelUtilisateur = new ApplicationUser
            {
                UserName = model.NomUtilisateur,
                Email = model.Email,
                Nom = model.Nom,
                Prenom = model.Prenom,
                EmailConfirmed = true,
                DoitChangerMotDePasse = true,
                CreeParId = adminPrincipalId
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

            await _userManager.AddToRoleAsync(nouvelUtilisateur, EcoTrack.Data.DbInitializer.RoleAdminSecondaire);

            var corpsEmail = $@"
                <p>Bonjour {model.Prenom} {model.Nom},</p>
                <p>Un compte administrateur EcoTrack a été créé pour vous.</p>
                <p><strong>Nom d'utilisateur :</strong> {model.NomUtilisateur}<br/>
                <strong>Mot de passe temporaire :</strong> {motDePasseGenere}</p>
                <p>Pour des raisons de sécurité, il vous sera demandé de changer ce mot de passe dès votre première connexion.</p>
                <p>— EcoTrack, Ecobank Togo</p>";

            try
            {
                await _emailSender.EnvoyerAsync(model.Email, "Votre compte EcoTrack a été créé", corpsEmail);
                TempData["Succes"] = $"L'administrateur \"{model.Prenom} {model.Nom}\" a été créé et un email lui a été envoyé avec ses identifiants.";
            }
            catch (Exception)
            {
                TempData["Erreur"] = $"L'administrateur a été créé, mais l'email n'a pas pu être envoyé. Mot de passe temporaire : {motDePasseGenere} (à communiquer manuellement).";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET /Utilisateurs/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            var utilisateur = await _userManager.FindByIdAsync(id);
            if (utilisateur is null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(utilisateur);
            if (roles.Contains(EcoTrack.Data.DbInitializer.RoleAdminPrincipal))
            {
                TempData["Erreur"] = "Vous ne pouvez pas modifier ce compte depuis cette page. Utilisez \"Mon profil\".";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new UtilisateurCreerViewModel
            {
                Nom = utilisateur.Nom,
                Prenom = utilisateur.Prenom,
                NomUtilisateur = utilisateur.UserName ?? string.Empty,
                Email = utilisateur.Email ?? string.Empty
            };

            ViewBag.UtilisateurId = utilisateur.Id;
            return View(viewModel);
        }

        // POST /Utilisateurs/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UtilisateurCreerViewModel model)
        {
            var utilisateur = await _userManager.FindByIdAsync(id);
            if (utilisateur is null)
            {
                return NotFound();
            }

            var nomUtilisateurNormalise = (model.NomUtilisateur ?? string.Empty).Trim().ToLower();
            var emailNormalise = (model.Email ?? string.Empty).Trim().ToLower();

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
                return View(model);
            }

            utilisateur.Nom = model.Nom;
            utilisateur.Prenom = model.Prenom;
            utilisateur.UserName = model.NomUtilisateur;
            utilisateur.NormalizedUserName = model.NomUtilisateur.ToUpper();
            utilisateur.Email = model.Email;
            utilisateur.NormalizedEmail = model.Email.ToUpper();

            var resultat = await _userManager.UpdateAsync(utilisateur);

            if (!resultat.Succeeded)
            {
                foreach (var erreur in resultat.Errors)
                {
                    ModelState.AddModelError(string.Empty, erreur.Description);
                }
                ViewBag.UtilisateurId = id;
                return View(model);
            }

            TempData["Succes"] = $"Les identifiants de \"{utilisateur.Prenom} {utilisateur.Nom}\" ont été mis à jour.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Utilisateurs/ReinitialiserMotDePasse/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReinitialiserMotDePasse(string id)
        {
            var utilisateur = await _userManager.FindByIdAsync(id);
            if (utilisateur is null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(utilisateur);
            if (roles.Contains(EcoTrack.Data.DbInitializer.RoleAdminPrincipal))
            {
                TempData["Erreur"] = "Impossible de réinitialiser le mot de passe de l'admin principal depuis cette page.";
                return RedirectToAction(nameof(Index));
            }

            var nouveauMotDePasse = GenerateurMotDePasse.Generer();

            var jeton = await _userManager.GeneratePasswordResetTokenAsync(utilisateur);
            var resultat = await _userManager.ResetPasswordAsync(utilisateur, jeton, nouveauMotDePasse);

            if (!resultat.Succeeded)
            {
                TempData["Erreur"] = "La réinitialisation a échoué.";
                return RedirectToAction(nameof(Index));
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

            return RedirectToAction(nameof(Index));
        }

        // POST /Utilisateurs/BasculerActivation/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BasculerActivation(string id)
        {
            var utilisateur = await _userManager.FindByIdAsync(id);
            if (utilisateur is null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(utilisateur);
            if (roles.Contains(EcoTrack.Data.DbInitializer.RoleAdminPrincipal))
            {
                TempData["Erreur"] = "Impossible de désactiver le compte administrateur principal.";
                return RedirectToAction(nameof(Index));
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

            return RedirectToAction(nameof(Index));
        }
    }
}