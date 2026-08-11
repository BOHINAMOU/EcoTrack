using EcoTrack.Data;
using EcoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Controllers
{
    [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
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
                .Include(d => d.Agence)
                .Include(d => d.Divisions)
                .OrderBy(d => d.Agence!.Nom).ThenBy(d => d.Nom)
                .ToListAsync();

            return View(departements);
        }

        // GET /Departements/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var departement = await _context.Departements
                .Include(d => d.Agence)
                .Include(d => d.Divisions)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (departement is null)
            {
                return NotFound();
            }

            return View(departement);
        }

        // GET /Departements/Create
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Agences = await _context.Agences.Where(a => a.EstActif).OrderBy(a => a.Nom).ToListAsync();
            return View();
        }

        // POST /Departements/Create
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Departement departement)
        {
            var existeDeja = await _context.Departements
                .AnyAsync(d => d.AgenceId == departement.AgenceId && d.Nom.ToLower() == departement.Nom.ToLower());

            if (existeDeja)
            {
                ModelState.AddModelError(nameof(Departement.Nom), "Un département avec ce nom existe déjà dans cette agence.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Agences = await _context.Agences.Where(a => a.EstActif).OrderBy(a => a.Nom).ToListAsync();
                return View(departement);
            }

            _context.Add(departement);
            await _context.SaveChangesAsync();

            TempData["Succes"] = $"Le département \"{departement.Nom}\" a été créé.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Departements/Edit/5
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        public async Task<IActionResult> Edit(int id)
        {
            var departement = await _context.Departements.FindAsync(id);

            if (departement is null)
            {
                return NotFound();
            }

            ViewBag.Agences = await _context.Agences.Where(a => a.EstActif).OrderBy(a => a.Nom).ToListAsync();
            return View(departement);
        }

        // POST /Departements/Edit/5
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Departement departement)
        {
            if (id != departement.Id)
            {
                return NotFound();
            }

            var existeDeja = await _context.Departements
                .AnyAsync(d => d.Id != departement.Id && d.AgenceId == departement.AgenceId && d.Nom.ToLower() == departement.Nom.ToLower());

            if (existeDeja)
            {
                ModelState.AddModelError(nameof(Departement.Nom), "Un département avec ce nom existe déjà dans cette agence.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Agences = await _context.Agences.Where(a => a.EstActif).OrderBy(a => a.Nom).ToListAsync();
                return View(departement);
            }

            _context.Update(departement);
            await _context.SaveChangesAsync();

            TempData["Succes"] = $"Le département \"{departement.Nom}\" a été modifié.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Departements/BasculerActivation/5
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
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