using EcoTrack.Data;
using EcoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace EcoTrack.Controllers
{
    [Authorize(Roles = "Employe")]
    public class MonEspaceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MonEspaceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET /MonEspace
        public async Task<IActionResult> Index()
        {
            var utilisateurId = _userManager.GetUserId(User);

            var employe = await _context.Employes
                .Include(e => e.Unite!)
                    .ThenInclude(u => u.Service!)
                        .ThenInclude(s => s.Division!)
                            .ThenInclude(d => d.Departement!)
                                .ThenInclude(dep => dep.Agence)
                .Include(e => e.Affectations.Where(a => a.DateRetrait == null))
                    .ThenInclude(a => a.Actif)
                .FirstOrDefaultAsync(e => e.ApplicationUserId == utilisateurId);

            if (employe is null)
            {
                return RedirectToAction("AccesRefuse", "Compte");
            }

            var mesActifs = employe.Affectations
                .Where(a => a.DateRetrait == null)
                .Select(a => a.Actif!)
                .OrderBy(a => a.Nom)
                .ToList();

            var agenceId = employe.Agence?.Id;
            var departementId = employe.DepartementOrg?.Id;
            var divisionId = employe.DivisionOrg?.Id;
            var serviceId = employe.ServiceOrg?.Id;
            var uniteId = employe.UniteId;
            var actifsAgence = new List<Actif>();

            if (agenceId.HasValue)
            {
                // Actifs rattachés directement à une unité de la hiérarchie de l'employé (partagés, pas attribués à quelqu'un de précis)
                var actifsPartages = await _context.Actifs
                    .Where(a => a.AgenceId == agenceId.Value
                        || (departementId.HasValue && a.DepartementId == departementId.Value)
                        || (divisionId.HasValue && a.DivisionId == divisionId.Value)
                        || (serviceId.HasValue && a.ServiceId == serviceId.Value)
                        || a.UniteId == uniteId)
                    .ToListAsync();

                // Actifs actuellement chez des collègues de la même agence
                var actifsCollegues = await _context.Affectations
                    .Include(a => a.Actif)
                    .Where(a => a.DateRetrait == null
                        && a.EmployeId != employe.Id
                        && a.Employe!.Unite!.Service!.Division!.Departement!.AgenceId == agenceId.Value)
                    .Select(a => a.Actif!)
                    .ToListAsync();

                actifsAgence = actifsPartages
                    .Concat(actifsCollegues)
                    .GroupBy(a => a.Id)
                    .Select(g => g.First())
                    .Where(a => !mesActifs.Select(m => m.Id).Contains(a.Id))
                    .OrderBy(a => a.Nom)
                    .ToList();
            }

            ViewBag.Employe = employe;
            ViewBag.MesActifs = mesActifs;
            ViewBag.ActifsAgence = actifsAgence;

            return View();
        }

        // GET /MonEspace/RapportPdf
        public async Task<IActionResult> RapportPdf()
        {
            var utilisateurId = _userManager.GetUserId(User);

            var employe = await _context.Employes
                .Include(e => e.Unite!)
                    .ThenInclude(u => u.Service!)
                        .ThenInclude(s => s.Division!)
                            .ThenInclude(d => d.Departement!)
                .Include(e => e.Service)
                .FirstOrDefaultAsync(e => e.ApplicationUserId == utilisateurId);

            if (employe is null)
            {
                return RedirectToAction("AccesRefuse", "Compte");
            }

            var affectations = await _context.Affectations
                .Include(a => a.Actif)
                    .ThenInclude(a => a!.CategorieActif)
                .Where(a => a.EmployeId == employe.Id)
                .OrderByDescending(a => a.DateAffectation)
                .ToListAsync();

            var actifIds = affectations.Select(a => a.ActifId).Distinct().ToList();
            var detenteursActuels = await _context.Affectations
                .Include(a => a.Employe)
                .Where(a => actifIds.Contains(a.ActifId) && a.DateRetrait == null)
                .ToDictionaryAsync(a => a.ActifId, a => a.Employe);

            var document = new RapportEmployeDocument(new List<Employe> { employe }, affectations, detenteursActuels);
            var pdf = document.GeneratePdf();

            return File(pdf, "application/pdf", $"Mes_Actifs_{employe.Nom}.pdf");
        }
    }
}