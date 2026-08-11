using EcoTrack.Data;
using EcoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Controllers
{
    [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
    public class AgencesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AgencesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /Agences
        public async Task<IActionResult> Index()
        {
            var agences = await _context.Agences
                .Include(a => a.Departements)
                .OrderBy(a => a.Nom)
                .ToListAsync();

            return View(agences);
        }

        // GET /Agences/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var agence = await _context.Agences
                .Include(a => a.Departements)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (agence is null)
            {
                return NotFound();
            }

            return View(agence);
        }

        // GET /Agences/Create
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        public IActionResult Create()
        {
            return View();
        }

        // POST /Agences/Create
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Agence agence)
        {
            var existeDeja = await _context.Agences
                .AnyAsync(a => a.Nom.ToLower() == agence.Nom.ToLower());

            if (existeDeja)
            {
                ModelState.AddModelError(nameof(Agence.Nom), "Une agence avec ce nom existe déjà.");
            }

            if (!ModelState.IsValid)
            {
                return View(agence);
            }

            _context.Add(agence);
            await _context.SaveChangesAsync();

            TempData["Succes"] = $"L'agence \"{agence.Nom}\" a été créée.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Agences/Edit/5
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        public async Task<IActionResult> Edit(int id)
        {
            var agence = await _context.Agences.FindAsync(id);

            if (agence is null)
            {
                return NotFound();
            }

            return View(agence);
        }

        // POST /Agences/Edit/5
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Agence agence)
        {
            if (id != agence.Id)
            {
                return NotFound();
            }

            var existeDeja = await _context.Agences
                .AnyAsync(a => a.Id != agence.Id && a.Nom.ToLower() == agence.Nom.ToLower());

            if (existeDeja)
            {
                ModelState.AddModelError(nameof(Agence.Nom), "Une agence avec ce nom existe déjà.");
            }

            if (!ModelState.IsValid)
            {
                return View(agence);
            }

            _context.Update(agence);
            await _context.SaveChangesAsync();

            TempData["Succes"] = $"L'agence \"{agence.Nom}\" a été modifiée.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Agences/BasculerActivation/5
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BasculerActivation(int id)
        {
            var agence = await _context.Agences.FindAsync(id);

            if (agence is null)
            {
                return NotFound();
            }

            agence.EstActif = !agence.EstActif;
            await _context.SaveChangesAsync();

            TempData["Succes"] = agence.EstActif
                ? $"L'agence \"{agence.Nom}\" a été réactivée."
                : $"L'agence \"{agence.Nom}\" a été désactivée.";

            return RedirectToAction(nameof(Index));
        }
    }
}