using EcoTrack.Data;
using EcoTrack.Infrastructure;
using EcoTrack.Models;
using EcoTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using IEmailSender = EcoTrack.Infrastructure.IEmailSender;

namespace EcoTrack.Controllers
{
    public class CompteController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public CompteController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Connexion()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Connexion(ConnexionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var resultat = await _signInManager.PasswordSignInAsync(
                model.NomUtilisateur,
                model.MotDePasse,
                model.SeSouvenirDeMoi,
                lockoutOnFailure: true);

            if (resultat.Succeeded)
            {
                var utilisateurConnecte = await _userManager.FindByNameAsync(model.NomUtilisateur);

                if (utilisateurConnecte is not null && utilisateurConnecte.DoitChangerMotDePasse)
                {
                    return RedirectToAction("ChangerMotDePasse");
                }

                return RedirectToAction("Index", "Home");
            }

            // --- Cas particulier : admin principal, 3 échecs -> réinitialisation automatique ---
            var utilisateurTente = await _userManager.FindByNameAsync(model.NomUtilisateur);
            if (utilisateurTente is not null)
            {
                var roles = await _userManager.GetRolesAsync(utilisateurTente);

                if (roles.Contains(DbInitializer.RoleAdminPrincipal))
                {
                    var nombreEchecs = await _userManager.GetAccessFailedCountAsync(utilisateurTente);

                    if (nombreEchecs >= 3)
                    {
                        var nouveauMotDePasse = GenerateurMotDePasse.Generer();
                        var jeton = await _userManager.GeneratePasswordResetTokenAsync(utilisateurTente);
                        await _userManager.ResetPasswordAsync(utilisateurTente, jeton, nouveauMotDePasse);
                        await _userManager.ResetAccessFailedCountAsync(utilisateurTente);
                        await _userManager.SetLockoutEndDateAsync(utilisateurTente, null);

                        utilisateurTente.DoitChangerMotDePasse = true;
                        await _userManager.UpdateAsync(utilisateurTente);

                        var corpsEmail = $@"
                            <p>Bonjour {utilisateurTente.Prenom} {utilisateurTente.Nom},</p>
                            <p>Après 3 tentatives de connexion échouées sur votre compte administrateur principal EcoTrack, votre mot de passe a été réinitialisé automatiquement par mesure de sécurité.</p>
                            <p><strong>Nom d'utilisateur :</strong> {utilisateurTente.UserName}<br/>
                            <strong>Nouveau mot de passe temporaire :</strong> {nouveauMotDePasse}</p>
                            <p>Il vous sera demandé de le changer dès votre prochaine connexion.</p>
                            <p>— EcoTrack, Ecobank Togo</p>";

                        try
                        {
                            await _emailSender.EnvoyerAsync(utilisateurTente.Email!, "Réinitialisation de sécurité - EcoTrack", corpsEmail);
                        }
                        catch (Exception)
                        {
                            // L'échec d'envoi ne doit pas empêcher la réinitialisation elle-même.
                        }

                        ModelState.AddModelError(string.Empty, "Trop de tentatives échouées. Un nouveau mot de passe temporaire vous a été envoyé par email.");
                        return View(model);
                    }
                }
            }

            if (resultat.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Compte temporairement verrouillé suite à plusieurs échecs. Réessayez dans 15 minutes.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Nom d'utilisateur ou mot de passe incorrect.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deconnexion()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Connexion");
        }

        [HttpGet]
        public IActionResult AccesRefuse()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangerMotDePasse()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangerMotDePasse(ChangerMotDePasseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var utilisateur = await _userManager.GetUserAsync(User);
            if (utilisateur is null)
            {
                return RedirectToAction("Connexion");
            }

            var resultat = await _userManager.ChangePasswordAsync(
                utilisateur,
                model.MotDePasseActuel,
                model.NouveauMotDePasse);

            if (!resultat.Succeeded)
            {
                foreach (var erreur in resultat.Errors)
                {
                    ModelState.AddModelError(string.Empty, erreur.Description);
                }
                return View(model);
            }

            utilisateur.DoitChangerMotDePasse = false;
            await _userManager.UpdateAsync(utilisateur);

            await _signInManager.RefreshSignInAsync(utilisateur);

            return RedirectToAction("Index", "Home");
        }

        // GET /Compte/Profil
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profil()
        {
            var utilisateur = await _userManager.GetUserAsync(User);
            if (utilisateur is null)
            {
                return RedirectToAction("Connexion");
            }

            var viewModel = new ProfilViewModel
            {
                Nom = utilisateur.Nom,
                Prenom = utilisateur.Prenom,
                NomUtilisateur = utilisateur.UserName ?? string.Empty,
                Email = utilisateur.Email ?? string.Empty
            };

            return View(viewModel);
        }

        // POST /Compte/Profil
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profil(ProfilViewModel model)
        {
            var utilisateur = await _userManager.GetUserAsync(User);
            if (utilisateur is null)
            {
                return RedirectToAction("Connexion");
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
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(utilisateur);

            TempData["Succes"] = "Vos informations ont été mises à jour.";
            return RedirectToAction("Index", "Home");
        }
    }
}