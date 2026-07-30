using EcoTrack.Models;
using EcoTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EcoTrack.Controllers
{
    public class CompteController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public CompteController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
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
                var utilisateur = await _userManager.FindByNameAsync(model.NomUtilisateur);

                if (utilisateur is not null && utilisateur.DoitChangerMotDePasse)
                {
                    return RedirectToAction("ChangerMotDePasse");
                }

                return RedirectToAction("Index", "Home");
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
    }
}