using EcoTrack.Data;
using EcoTrack.Enums;
using EcoTrack.Infrastructure;
using EcoTrack.Models;
using EcoTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace EcoTrack.Controllers
{
    [Authorize]
    public class ActifsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IJournalService _journal;

        public ActifsController(ApplicationDbContext context, IJournalService journal)
        {
            _context = context;
            _journal = journal;
        }

        private string UtilisateurConnecteId => User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? string.Empty;

        // GET /Actifs?etat=Disponible
        public async Task<IActionResult> Index(string? etat)
        {
            var requete = _context.Actifs
                .Include(a => a.CategorieActif)
                .Include(a => a.Affectations.Where(aff => aff.DateRetrait == null))
                    .ThenInclude(aff => aff.Employe)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(etat) && Enum.TryParse<EtatActif>(etat, true, out var etatFiltre))
            {
                requete = requete.Where(a => a.Etat == etatFiltre);
            }

            var actifs = await requete.OrderBy(a => a.Nom).ToListAsync();

            ViewBag.FiltreActuel = etat;
            return View(actifs);
        }

        // GET /Actifs/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var actif = await _context.Actifs
                .Include(a => a.CategorieActif)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (actif is null)
            {
                return NotFound();
            }

            var historique = await _context.Affectations
                .Include(af => af.Employe)
                .Where(af => af.ActifId == id)
                .OrderBy(af => af.DateAffectation)
                .ToListAsync();

            var lignes = historique.Select((aff, index) =>
            {
                var precedente = index > 0 ? historique[index - 1] : null;

                return new HistoriqueLigneViewModel
                {
                    Affectation = aff,
                    EstReattribution = precedente is not null,
                    DetenteurPrecedentNom = precedente is not null ? $"{precedente.Employe?.Prenom} {precedente.Employe?.Nom}" : null,
                    MemeEmployeQuAvant = precedente is not null && precedente.EmployeId == aff.EmployeId
                };
            })
            .OrderByDescending(l => l.Affectation.DateAffectation)
            .ToList();

            ViewBag.Historique = lignes;
            return View(actif);
        }

        // GET /Actifs/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new ActifFormViewModel
            {
                Categories = await _context.CategoriesActifs.Where(c => c.EstActif).OrderBy(c => c.Nom).ToListAsync()
            };

            return View(viewModel);
        }

        // POST /Actifs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActifFormViewModel model)
        {
            var numeroSerieNormalise = (model.NumeroSerie ?? string.Empty).Trim().ToLower();

            var existeDeja = !string.IsNullOrWhiteSpace(numeroSerieNormalise)
                && await _context.Actifs.AnyAsync(a => a.NumeroSerie.ToLower() == numeroSerieNormalise);

            if (existeDeja)
            {
                ModelState.AddModelError(nameof(model.NumeroSerie), "Ce numéro de série existe déjà.");
            }

            if (!ModelState.IsValid)
            {
                model.Categories = await _context.CategoriesActifs.Where(c => c.EstActif).OrderBy(c => c.Nom).ToListAsync();
                return View(model);
            }

            var actif = new Actif
            {
                Nom = model.Nom,
                NumeroSerie = model.NumeroSerie,
                Marque = model.Marque,
                Modele = model.Modele,
                DateAcquisition = DateTime.SpecifyKind(model.DateAcquisition, DateTimeKind.Utc),
                CategorieActifId = model.CategorieActifId,
                Etat = EtatActif.Disponible
            };

            _context.Actifs.Add(actif);
            await _context.SaveChangesAsync();

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "CreationActif",
                $"A enregistré un nouvel actif \"{actif.Nom}\" (N° série : {actif.NumeroSerie})");

            TempData["Succes"] = $"L'actif \"{actif.Nom}\" a été enregistré et est disponible.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Actifs/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var actif = await _context.Actifs.FindAsync(id);
            if (actif is null)
            {
                return NotFound();
            }

            var viewModel = new ActifFormViewModel
            {
                Id = actif.Id,
                Nom = actif.Nom,
                NumeroSerie = actif.NumeroSerie,
                Marque = actif.Marque,
                Modele = actif.Modele,
                DateAcquisition = actif.DateAcquisition,
                CategorieActifId = actif.CategorieActifId,
                Categories = await _context.CategoriesActifs.Where(c => c.EstActif).OrderBy(c => c.Nom).ToListAsync()
            };

            return View(viewModel);
        }

        // POST /Actifs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ActifFormViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var numeroSerieNormalise = (model.NumeroSerie ?? string.Empty).Trim().ToLower();

            var existeDeja = !string.IsNullOrWhiteSpace(numeroSerieNormalise)
                && await _context.Actifs.AnyAsync(a => a.Id != model.Id && a.NumeroSerie.ToLower() == numeroSerieNormalise);

            if (existeDeja)
            {
                ModelState.AddModelError(nameof(model.NumeroSerie), "Ce numéro de série existe déjà.");
            }

            if (!ModelState.IsValid)
            {
                model.Categories = await _context.CategoriesActifs.Where(c => c.EstActif).OrderBy(c => c.Nom).ToListAsync();
                return View(model);
            }

            var actif = await _context.Actifs.FindAsync(id);
            if (actif is null)
            {
                return NotFound();
            }

            actif.Nom = model.Nom;
            actif.NumeroSerie = model.NumeroSerie;
            actif.Marque = model.Marque;
            actif.Modele = model.Modele;
            actif.DateAcquisition = DateTime.SpecifyKind(model.DateAcquisition, DateTimeKind.Utc);
            actif.CategorieActifId = model.CategorieActifId;

            await _context.SaveChangesAsync();

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "ModificationActif",
                $"A modifié l'actif \"{actif.Nom}\" (N° série : {actif.NumeroSerie})");

            TempData["Succes"] = $"L'actif \"{actif.Nom}\" a été modifié.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Actifs/MarquerDeteriore/5
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarquerDeteriore(int id)
        {
            var actif = await _context.Actifs.FindAsync(id);
            if (actif is null)
            {
                return NotFound();
            }

            if (actif.Etat == EtatActif.Attribue)
            {
                TempData["Erreur"] = "Impossible de marquer cet actif comme détérioré : il est actuellement attribué à un employé. Retirez-le d'abord.";
                return RedirectToAction(nameof(Index));
            }

            actif.Etat = EtatActif.Deteriore;
            await _context.SaveChangesAsync();

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "DeteriorationActif",
                $"A marqué l'actif \"{actif.Nom}\" comme détérioré");

            TempData["Succes"] = $"L'actif \"{actif.Nom}\" a été marqué comme détérioré.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Actifs/Reactiver/5
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactiver(int id)
        {
            var actif = await _context.Actifs.FindAsync(id);
            if (actif is null)
            {
                return NotFound();
            }

            if (actif.Etat != EtatActif.Deteriore)
            {
                return RedirectToAction(nameof(Index));
            }

            actif.Etat = EtatActif.Disponible;
            await _context.SaveChangesAsync();

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "ReactivationActif",
                $"A remis l'actif \"{actif.Nom}\" disponible");

            TempData["Succes"] = $"L'actif \"{actif.Nom}\" a été remis disponible.";
            return RedirectToAction(nameof(Index));
        }
    }
}