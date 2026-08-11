using EcoTrack.Data;
using EcoTrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EcoTrack.Infrastructure
{
    /// <summary>
    /// Vérifie à chaque requête authentifiée si le compte a une date d'expiration d'accès dépassée.
    /// Si oui : retire le rôle AdminTemporaire en base et déconnecte immédiatement l'utilisateur
    /// (sans ça, quelqu'un déjà connecté garderait ses droits jusqu'à sa prochaine connexion).
    /// </summary>
    public class ExpirationAccesFilter : IAsyncActionFilter
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ExpirationAccesFilter(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.User.Identity is { IsAuthenticated: true })
            {
                var utilisateur = await _userManager.GetUserAsync(context.HttpContext.User);

                if (utilisateur is not null
                    && utilisateur.DateExpirationAcces is not null
                    && utilisateur.DateExpirationAcces.Value < DateTime.UtcNow)
                {
                    if (await _userManager.IsInRoleAsync(utilisateur, DbInitializer.RoleAdminTemporaire))
                    {
                        await _userManager.RemoveFromRoleAsync(utilisateur, DbInitializer.RoleAdminTemporaire);
                    }

                    utilisateur.DateExpirationAcces = null;
                    await _userManager.UpdateAsync(utilisateur);

                    await _signInManager.SignOutAsync();

                    context.Result = new Microsoft.AspNetCore.Mvc.RedirectToActionResult("Connexion", "Compte", null);
                    return;
                }
            }

            await next();
        }
    }
}