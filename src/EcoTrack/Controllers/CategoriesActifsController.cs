using EcoTrack.Data;
using EcoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Controllers
{
    [Authorize]
    public class CategoriesActifsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesActifsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /CategoriesActifs
        public async Task<IActionResult> Index()
        {
            var categories = await _context.CategoriesActifs
                .Include(c => c.Actifs)
                .OrderBy(c => c.Nom)
                .ToListAsync();

            return View(categories);
        }

        // GET /CategoriesActifs/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var categorie = await _context.CategoriesActifs
                .Include(c => c.Actifs)
                    .ThenInclude(a => a.Departement)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (categorie is null)
            {
                return NotFound();
            }

            return View(categorie);
        }

        // GET /CategoriesActifs/Create
        [Authorize(Roles = "AdminPrincipal")]
        public IActionResult Create()
        {
            return View();
        }

        // POST /CategoriesActifs/Create
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategorieActif categorie)
        {
            // Vérification si le Nom a été renseigné
            if (!string.IsNullOrWhiteSpace(categorie.Nom))
            {
                var nomSaisi = categorie.Nom.Trim();

                // Requête sécurisée contre les valeurs nulles
                var existeDeja = await _context.CategoriesActifs
                    .AnyAsync(c => c.Nom != null && c.Nom.ToLower() == nomSaisi.ToLower());

                if (existeDeja)
                {
                    ModelState.AddModelError(nameof(CategorieActif.Nom), "Une catégorie avec ce nom existe déjà.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(categorie);
            }

            _context.Add(categorie);
            await _context.SaveChangesAsync();

            TempData["Succes"] = $"La catégorie \"{categorie.Nom}\" a été créée.";
            return RedirectToAction(nameof(Index));
        }

        // GET /CategoriesActifs/Edit/5
        [Authorize(Roles = "AdminPrincipal")]
        public async Task<IActionResult> Edit(int id)
        {
            var categorie = await _context.CategoriesActifs.FindAsync(id);

            if (categorie is null)
            {
                return NotFound();
            }

            return View(categorie);
        }

        // POST /CategoriesActifs/Edit/5
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategorieActif categorie)
        {
            if (id != categorie.Id)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(categorie.Nom))
            {
                var nomSaisi = categorie.Nom.Trim();

                // Requête sécurisée également dans Edit
                var existeDeja = await _context.CategoriesActifs
                    .AnyAsync(c => c.Id != categorie.Id && c.Nom != null && c.Nom.ToLower() == nomSaisi.ToLower());

                if (existeDeja)
                {
                    ModelState.AddModelError(nameof(CategorieActif.Nom), "Une catégorie avec ce nom existe déjà.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(categorie);
            }

            _context.Update(categorie);
            await _context.SaveChangesAsync();

            TempData["Succes"] = $"La catégorie \"{categorie.Nom}\" a été modifiée.";
            return RedirectToAction(nameof(Index));
        }

        // POST /CategoriesActifs/BasculerActivation/5
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BasculerActivation(int id)
        {
            var categorie = await _context.CategoriesActifs.FindAsync(id);

            if (categorie is null)
            {
                return NotFound();
            }

            categorie.EstActif = !categorie.EstActif;
            await _context.SaveChangesAsync();

            TempData["Succes"] = categorie.EstActif
                ? $"La catégorie \"{categorie.Nom}\" a été réactivée."
                : $"La catégorie \"{categorie.Nom}\" a été désactivée.";

            return RedirectToAction(nameof(Index));
        }
    }
}