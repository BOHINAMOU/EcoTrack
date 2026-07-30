using EcoTrack.Data;
using EcoTrack.Enums;
using EcoTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel
            {
                NombreEmployes = await _context.Employes.CountAsync(e => e.EstActif),
                NombreActifsDisponibles = await _context.Actifs.CountAsync(a => a.Etat == EtatActif.Disponible),
                NombreActifsAttribues = await _context.Actifs.CountAsync(a => a.Etat == EtatActif.Attribue),
                NombreActifsDetruits = await _context.Actifs.CountAsync(a => a.Etat == EtatActif.Detruit)
            };

            return View(viewModel);
        }
    }
}