using EcoTrack.Data;
using EcoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Controllers
{
    [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
    public class UnitesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UnitesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /Unites
        public async Task<IActionResult> Index()
        {
            var unites = await _context.Unites
                .Include(u => u.Service)
                .Include(u => u.Employes)
                .OrderBy(u => u.Service!.Nom).ThenBy(u => u.Nom)
                .ToListAsync();

            return View(unites);
        }

        // GET /Unites/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var unite = await _context.Unites
                .Include(u => u.Service)
                .Include(u => u.Employes)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (unite is null)
            {
                return NotFound();
            }

            return View(unite);
        }

        // GET /Unites/Create
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Services = await _context.Services.Where(s => s.EstActif).OrderBy(s => s.Nom).ToListAsync();
            return View();
        }

        // POST /Unites/Create
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Unite unite)
        {
            var nomNormalise = (unite.Nom ?? string.Empty).Trim().ToLower();

            var existeDeja = await _context.Unites
                .AnyAsync(u => u.ServiceId == unite.ServiceId && u.Nom.ToLower() == nomNormalise);

            if (existeDeja)
            {
                ModelState.AddModelError(nameof(Unite.Nom), "Une unité avec ce nom existe déjà dans ce service.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Services = await _context.Services.Where(s => s.EstActif).OrderBy(s => s.Nom).ToListAsync();
                return View(unite);
            }

            _context.Add(unite);
            await _context.SaveChangesAsync();

            TempData["Succes"] = $"L'unité \"{unite.Nom}\" a été créée.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Unites/Edit/5
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        public async Task<IActionResult> Edit(int id)
        {
            var unite = await _context.Unites.FindAsync(id);

            if (unite is null)
            {
                return NotFound();
            }

            ViewBag.Services = await _context.Services.Where(s => s.EstActif).OrderBy(s => s.Nom).ToListAsync();
            return View(unite);
        }

        // POST /Unites/Edit/5
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Unite unite)
        {
            if (id != unite.Id)
            {
                return NotFound();
            }

            var nomNormalise = (unite.Nom ?? string.Empty).Trim().ToLower();

            var existeDeja = await _context.Unites
                .AnyAsync(u => u.Id != unite.Id && u.ServiceId == unite.ServiceId && u.Nom.ToLower() == nomNormalise);

            if (existeDeja)
            {
                ModelState.AddModelError(nameof(Unite.Nom), "Une unité avec ce nom existe déjà dans ce service.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Services = await _context.Services.Where(s => s.EstActif).OrderBy(s => s.Nom).ToListAsync();
                return View(unite);
            }

            _context.Update(unite);
            await _context.SaveChangesAsync();

            TempData["Succes"] = $"L'unité \"{unite.Nom}\" a été modifiée.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Unites/BasculerActivation/5
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BasculerActivation(int id)
        {
            var unite = await _context.Unites.FindAsync(id);

            if (unite is null)
            {
                return NotFound();
            }

            unite.EstActif = !unite.EstActif;
            await _context.SaveChangesAsync();

            TempData["Succes"] = unite.EstActif
                ? $"L'unité \"{unite.Nom}\" a été réactivée."
                : $"L'unité \"{unite.Nom}\" a été désactivée.";

            return RedirectToAction(nameof(Index));
        }
    }
}