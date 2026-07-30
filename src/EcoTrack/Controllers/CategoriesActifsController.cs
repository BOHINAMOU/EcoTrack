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

        // GET /CategoriesActifs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST /CategoriesActifs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategorieActif categorie)
        {
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategorieActif categorie)
        {
            if (id != categorie.Id)
            {
                return NotFound();
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