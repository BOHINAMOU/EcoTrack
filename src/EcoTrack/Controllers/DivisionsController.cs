using EcoTrack.Data;
using EcoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Controllers
{
    [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
    public class DivisionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DivisionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /Divisions
        public async Task<IActionResult> Index()
        {
            var divisions = await _context.Divisions
                .Include(d => d.Departement)
                    .ThenInclude(dep => dep!.Agence)
                .Include(d => d.Services)
                .OrderBy(d => d.Departement!.Nom).ThenBy(d => d.Nom)
                .ToListAsync();

            return View(divisions);
        }

        // GET /Divisions/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var division = await _context.Divisions
                .Include(d => d.Departement)
                .Include(d => d.Services)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (division is null)
            {
                return NotFound();
            }

            return View(division);
        }

        // GET /Divisions/Create
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Departements = await _context.Departements
                .Include(d => d.Agence)
                .Where(d => d.EstActif)
                .OrderBy(d => d.Agence!.Nom).ThenBy(d => d.Nom)
                .ToListAsync();
            return View();
        }

        // POST /Divisions/Create
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Division division)
        {
            var existeDeja = await _context.Divisions
                .AnyAsync(d => d.DepartementId == division.DepartementId && d.Nom.ToLower() == division.Nom.ToLower());

            if (existeDeja)
            {
                ModelState.AddModelError(nameof(Division.Nom), "Une division avec ce nom existe déjà dans ce département.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Departements = await _context.Departements.Include(d => d.Agence).Where(d => d.EstActif).OrderBy(d => d.Agence!.Nom).ThenBy(d => d.Nom).ToListAsync();
                return View(division);
            }

            _context.Add(division);
            await _context.SaveChangesAsync();

            TempData["Succes"] = $"La division \"{division.Nom}\" a été créée.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Divisions/Edit/5
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        public async Task<IActionResult> Edit(int id)
        {
            var division = await _context.Divisions.FindAsync(id);

            if (division is null)
            {
                return NotFound();
            }

            ViewBag.Departements = await _context.Departements.Include(d => d.Agence).Where(d => d.EstActif).OrderBy(d => d.Agence!.Nom).ThenBy(d => d.Nom).ToListAsync();
            return View(division);
        }

        // POST /Divisions/Edit/5
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Division division)
        {
            if (id != division.Id)
            {
                return NotFound();
            }

            var existeDeja = await _context.Divisions
                .AnyAsync(d => d.Id != division.Id && d.DepartementId == division.DepartementId && d.Nom.ToLower() == division.Nom.ToLower());

            if (existeDeja)
            {
                ModelState.AddModelError(nameof(Division.Nom), "Une division avec ce nom existe déjà dans ce département.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Departements = await _context.Departements.Include(d => d.Agence).Where(d => d.EstActif).OrderBy(d => d.Agence!.Nom).ThenBy(d => d.Nom).ToListAsync();
                return View(division);
            }

            _context.Update(division);
            await _context.SaveChangesAsync();

            TempData["Succes"] = $"La division \"{division.Nom}\" a été modifiée.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Divisions/BasculerActivation/5
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BasculerActivation(int id)
        {
            var division = await _context.Divisions.FindAsync(id);

            if (division is null)
            {
                return NotFound();
            }

            division.EstActif = !division.EstActif;
            await _context.SaveChangesAsync();

            TempData["Succes"] = division.EstActif
                ? $"La division \"{division.Nom}\" a été réactivée."
                : $"La division \"{division.Nom}\" a été désactivée.";

            return RedirectToAction(nameof(Index));
        }
    }
}