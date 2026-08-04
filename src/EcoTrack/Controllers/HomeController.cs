using EcoTrack.Data;
using EcoTrack.Enums;
using EcoTrack.Models;
using EcoTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var utilisateur = await _userManager.GetUserAsync(User);

            var viewModel = new DashboardViewModel
            {
                PrenomUtilisateur = utilisateur?.Prenom ?? "",
                NomUtilisateur = utilisateur?.Nom ?? "",
                NombreEmployes = await _context.Employes.CountAsync(e => e.EstActif),
                NombreActifsDisponibles = await _context.Actifs.CountAsync(a => a.Etat == EtatActif.Disponible),
                NombreActifsAttribues = await _context.Actifs.CountAsync(a => a.Etat == EtatActif.Attribue),
                NombreActifsDeteriores = await _context.Actifs.CountAsync(a => a.Etat == EtatActif.Deteriore)
            };

            return View(viewModel);
        }

        // GET /Home/Rechercher?terme=...
        [HttpGet]
        public async Task<IActionResult> Rechercher(string? terme)
        {
            if (string.IsNullOrWhiteSpace(terme) || terme.Trim().Length < 2)
            {
                return Json(new { employes = Array.Empty<object>(), actifs = Array.Empty<object>() });
            }

            var termeNormalise = terme.Trim().ToLower();

            var employes = await _context.Employes
                .Include(e => e.Departement)
                .Where(e => (e.Nom + " " + e.Prenom).ToLower().Contains(termeNormalise)
                         || (e.Prenom + " " + e.Nom).ToLower().Contains(termeNormalise))
                .Take(8)
                .Select(e => new
                {
                    id = e.Id,
                    nomComplet = e.Prenom + " " + e.Nom,
                    departement = e.Departement != null ? e.Departement.Nom : "—"
                })
                .ToListAsync();

            var actifs = await _context.Actifs
                .Include(a => a.Affectations.Where(aff => aff.DateRetrait == null))
                    .ThenInclude(aff => aff.Employe)
                .Where(a => a.Nom.ToLower().Contains(termeNormalise) || a.NumeroSerie.ToLower().Contains(termeNormalise))
                .Take(8)
                .Select(a => new
                {
                    id = a.Id,
                    nom = a.Nom,
                    numeroSerie = a.NumeroSerie,
                    etat = a.Etat.ToString(),
                    employeActuel = a.Affectations.Where(aff => aff.DateRetrait == null).Select(aff => aff.Employe!.Prenom + " " + aff.Employe!.Nom).FirstOrDefault(),
                    employeId = a.Affectations.Where(aff => aff.DateRetrait == null).Select(aff => (int?)aff.EmployeId).FirstOrDefault()
                })
                .ToListAsync();

            return Json(new { employes, actifs });
        }
    }
}