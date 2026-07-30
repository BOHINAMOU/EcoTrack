using EcoTrack.Data;
using EcoTrack.Models;
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
                .Include(d => d.Actifs)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (departement is null)
            {
                return NotFound();
            }

            return View(departement);
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