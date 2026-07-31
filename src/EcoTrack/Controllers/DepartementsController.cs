using EcoTrack.Data;
using EcoTrack.Models;
using EcoTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Controllers
{
    [Authorize]
    public class DepartementsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartementsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /Departements
        public async Task<IActionResult> Index()
        {
            var departements = await _context.Departements
                .Include(d => d.Employes)
                .Include(d => d.Actifs)
                .OrderBy(d => d.Nom)
                .ToListAsync();

            return View(departements);
        }

        // GET /Departements/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var departement = await _context.Departements
                .Include(d => d.Employes)
                    .ThenInclude(e => e.Affectations.Where(a => a.DateRetrait == null))
                        .ThenInclude(a => a.Actif)
                            .ThenInclude(actif => actif!.CategorieActif)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (departement is null)
            {
                return NotFound();
            }

            var actifsDetenus = departement.Employes
                .SelectMany(e => e.Affectations)
                .Select(a => a.Actif)
                .Where(a => a is not null)
                .Select(a => a!)
                .ToList();

            var palette = new[] { "#0d3b66", "#ffc107", "#2e8540", "#d9534f", "#6f42c1", "#17a2b8", "#fd7e14" };

            var repartition = actifsDetenus
                .GroupBy(a => a.CategorieActif?.Nom ?? "Non catégorisé")
                .Select((groupe, index) => new RepartitionCategorie
                {
                    NomCategorie = groupe.Key,
                    Nombre = groupe.Count(),
                    Pourcentage = actifsDetenus.Count == 0
                        ? 0
                        : Math.Round(groupe.Count() * 100.0 / actifsDetenus.Count, 1),
                    CouleurHex = palette[index % palette.Length]
                })
                .OrderByDescending(r => r.Nombre)
                .ToList();

            var employesIds = departement.Employes.Select(e => e.Id).ToList();

            var dernieresAffectations = await _context.Affectations
                .Include(a => a.Actif)
                .Include(a => a.Employe)
                .Where(a => employesIds.Contains(a.EmployeId))
                .OrderByDescending(a => a.DateAffectation)
                .Take(5)
                .ToListAsync();

            var viewModel = new DepartementDetailsViewModel
            {
                Departement = departement,
                RepartitionParCategorie = repartition,
                DernieresAffectations = dernieresAffectations
            };

            return View(viewModel);
        }

        // GET /Departements/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST /Departements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Departement departement)
        {
            var existeDeja = await _context.Departements
                .AnyAsync(d => d.Nom.ToLower() == departement.Nom.ToLower());

            if (existeDeja)
            {
                ModelState.AddModelError(nameof(Departement.Nom), "Un département avec ce nom existe déjà.");
            }

            if (!ModelState.IsValid)
            {
                return View(departement);
            }

            _context.Add(departement);
            await _context.SaveChangesAsync();

            TempData["Succes"] = $"Le département \"{departement.Nom}\" a été créé.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Departements/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var departement = await _context.Departements.FindAsync(id);

            if (departement is null)
            {
                return NotFound();
            }

            return View(departement);
        }

        // POST /Departements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Departement departement)
        {
            if (id != departement.Id)
            {
                return NotFound();
            }

            var existeDeja = await _context.Departements
                .AnyAsync(d => d.Id != departement.Id && d.Nom.ToLower() == departement.Nom.ToLower());

            if (existeDeja)
            {
                ModelState.AddModelError(nameof(Departement.Nom), "Un département avec ce nom existe déjà.");
            }

            if (!ModelState.IsValid)
            {
                return View(departement);
            }

            _context.Update(departement);
            await _context.SaveChangesAsync();

            TempData["Succes"] = $"Le département \"{departement.Nom}\" a été modifié.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Departements/BasculerActivation/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BasculerActivation(int id)
        {
            var departement = await _context.Departements.FindAsync(id);

            if (departement is null)
            {
                return NotFound();
            }

            departement.EstActif = !departement.EstActif;
            await _context.SaveChangesAsync();

            TempData["Succes"] = departement.EstActif
                ? $"Le département \"{departement.Nom}\" a été réactivé."
                : $"Le département \"{departement.Nom}\" a été désactivé.";

            return RedirectToAction(nameof(Index));
        }
    }
}