using EcoTrack.Data;
using EcoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Controllers
{
    [Authorize]
    public class ServicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /Services
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services
                .Include(s => s.Departement)
                .Include(s => s.Employes)
                .OrderBy(s => s.Departement!.Nom).ThenBy(s => s.Nom)
                .ToListAsync();

            return View(services);
        }

        // GET /Services/Create
        [Authorize(Roles = "AdminPrincipal")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Agences = await _context.Departements.Where(d => d.EstActif).OrderBy(d => d.Nom).ToListAsync();
            return View();
        }

        // POST /Services/Create
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service service)
        {
            var existeDeja = await _context.Services
                .AnyAsync(s => s.DepartementId == service.DepartementId && s.Nom.ToLower() == service.Nom.ToLower());

            if (existeDeja)
            {
                ModelState.AddModelError(nameof(Service.Nom), "Un service avec ce nom existe déjà dans cette agence.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Agences = await _context.Departements.Where(d => d.EstActif).OrderBy(d => d.Nom).ToListAsync();
                return View(service);
            }

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            TempData["Succes"] = $"Le service \"{service.Nom}\" a été créé.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Services/Edit/5
        [Authorize(Roles = "AdminPrincipal")]
        public async Task<IActionResult> Edit(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service is null)
            {
                return NotFound();
            }

            ViewBag.Agences = await _context.Departements.Where(d => d.EstActif).OrderBy(d => d.Nom).ToListAsync();
            return View(service);
        }

        // POST /Services/Edit/5
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Service service)
        {
            if (id != service.Id)
            {
                return NotFound();
            }

            var existeDeja = await _context.Services
                .AnyAsync(s => s.Id != service.Id && s.DepartementId == service.DepartementId && s.Nom.ToLower() == service.Nom.ToLower());

            if (existeDeja)
            {
                ModelState.AddModelError(nameof(Service.Nom), "Un service avec ce nom existe déjà dans cette agence.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Agences = await _context.Departements.Where(d => d.EstActif).OrderBy(d => d.Nom).ToListAsync();
                return View(service);
            }

            _context.Update(service);
            await _context.SaveChangesAsync();

            TempData["Succes"] = $"Le service \"{service.Nom}\" a été modifié.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Services/BasculerActivation/5
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BasculerActivation(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service is null)
            {
                return NotFound();
            }

            service.EstActif = !service.EstActif;
            await _context.SaveChangesAsync();

            TempData["Succes"] = service.EstActif
                ? $"Le service \"{service.Nom}\" a été réactivé."
                : $"Le service \"{service.Nom}\" a été désactivé.";

            return RedirectToAction(nameof(Index));
        }
    }
}