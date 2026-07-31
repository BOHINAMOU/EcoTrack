using EcoTrack.Data;
using EcoTrack.Enums;
using EcoTrack.Models;
using EcoTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Controllers
{
    [Authorize]
    public class ActifsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ActifsController(ApplicationDbContext context)
        {
            _context = context;
        }

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
                .OrderByDescending(af => af.DateAffectation)
                .ToListAsync();

            ViewBag.Historique = historique;
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
            var existeDeja = await _context.Actifs
                .AnyAsync(a => a.NumeroSerie.ToLower() == model.NumeroSerie.ToLower());

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
                DateAcquisition = model.DateAcquisition,
                CategorieActifId = model.CategorieActifId,
                Etat = EtatActif.Disponible
            };

            _context.Actifs.Add(actif);
            await _context.SaveChangesAsync();

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

            var existeDeja = await _context.Actifs
                .AnyAsync(a => a.Id != model.Id && a.NumeroSerie.ToLower() == model.NumeroSerie.ToLower());

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

            // On ne touche jamais à Etat ici : ça reste géré uniquement par les actions d'attribution/destruction.
            actif.Nom = model.Nom;
            actif.NumeroSerie = model.NumeroSerie;
            actif.Marque = model.Marque;
            actif.Modele = model.Modele;
            actif.DateAcquisition = model.DateAcquisition;
            actif.CategorieActifId = model.CategorieActifId;

            await _context.SaveChangesAsync();

            TempData["Succes"] = $"L'actif \"{actif.Nom}\" a été modifié.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Actifs/MarquerDetruit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarquerDetruit(int id)
        {
            var actif = await _context.Actifs.FindAsync(id);
            if (actif is null)
            {
                return NotFound();
            }

            if (actif.Etat == EtatActif.Attribue)
            {
                TempData["Erreur"] = "Impossible de marquer cet actif comme détruit : il est actuellement attribué à un employé. Retirez-le d'abord.";
                return RedirectToAction(nameof(Index));
            }

            actif.Etat = EtatActif.Detruit;
            await _context.SaveChangesAsync();

            TempData["Succes"] = $"L'actif \"{actif.Nom}\" a été marqué comme détruit.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Actifs/Reactiver/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactiver(int id)
        {
            var actif = await _context.Actifs.FindAsync(id);
            if (actif is null)
            {
                return NotFound();
            }

            if (actif.Etat != EtatActif.Detruit)
            {
                return RedirectToAction(nameof(Index));
            }

            actif.Etat = EtatActif.Disponible;
            await _context.SaveChangesAsync();

            TempData["Succes"] = $"L'actif \"{actif.Nom}\" a été remis disponible.";
            return RedirectToAction(nameof(Index));
        }
    }
}