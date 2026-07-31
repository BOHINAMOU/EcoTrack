using EcoTrack.Data;
using EcoTrack.Enums;
using EcoTrack.Models;
using EcoTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Controllers
{
    [Authorize]
    public class EmployesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /Employes
        public async Task<IActionResult> Index()
        {
            var employes = await _context.Employes
                .Include(e => e.Departement)
                .Include(e => e.Affectations.Where(a => a.DateRetrait == null))
                .OrderBy(e => e.Nom)
                .ToListAsync();

            return View(employes);
        }
        // GET /Employes/Rechercher
        [HttpGet]
        public IActionResult Rechercher()
        {
            return View();
        }

        // GET /Employes/RechercherJson?terme=...
        [HttpGet]
        public async Task<IActionResult> RechercherJson(string? terme)
        {
            if (string.IsNullOrWhiteSpace(terme) || terme.Trim().Length < 2)
            {
                return Json(Array.Empty<object>());
            }

            var termeNormalise = terme.Trim().ToLower();

            var resultats = await _context.Employes
                .Include(e => e.Departement)
                .Where(e => (e.Prenom + " " + e.Nom).ToLower().Contains(termeNormalise)
                         || (e.Nom + " " + e.Prenom).ToLower().Contains(termeNormalise)
                         || (e.Poste != null && e.Poste.ToLower().Contains(termeNormalise)))
                .OrderBy(e => e.Nom)
                .Take(20)
                .Select(e => new
                {
                    id = e.Id,
                    nomComplet = e.Prenom + " " + e.Nom,
                    departement = e.Departement != null ? e.Departement.Nom : "—",
                    poste = e.Poste ?? "—",
                    estActif = e.EstActif,
                    nombreActifs = e.Affectations.Count(a => a.DateRetrait == null)
                })
                .ToListAsync();

            return Json(resultats);
        }

        // GET /Employes/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new EmployeCreerViewModel
            {
                Departements = await _context.Departements.Where(d => d.EstActif).OrderBy(d => d.Nom).ToListAsync(),
                ActifsDisponibles = await _context.Actifs.Where(a => a.Etat == EtatActif.Disponible).OrderBy(a => a.Nom).ToListAsync(),
                Categories = await _context.CategoriesActifs.Where(c => c.EstActif).OrderBy(c => c.Nom).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeCreerViewModel model)
        {
            var telephoneComplet = $"{model.Indicatif} {model.NumeroTelephone}".Trim();
            var nomNormalise = (model.Nom ?? string.Empty).Trim().ToLower();
            var prenomNormalise = (model.Prenom ?? string.Empty).Trim().ToLower();
            var emailNormalise = model.Email?.Trim().ToLower();

            // On récupère d'abord les candidats par Nom+Prénom (requête SQL simple),
            // puis on compare l'email en mémoire côté C# pour éviter une expression
            // trop complexe que EF Core traduirait mal.
            var emailsExistants = await _context.Employes
                .Where(e => e.Nom.ToLower() == nomNormalise && e.Prenom.ToLower() == prenomNormalise)
                .Select(e => e.Email)
                .ToListAsync();

            var existeDeja = emailsExistants.Any(email =>
                (email == null && emailNormalise == null) ||
                (email != null && emailNormalise != null && email.ToLower() == emailNormalise));

            if (existeDeja)
            {
                ModelState.AddModelError(string.Empty, "Un employé avec ce nom, ce prénom et cet email existe déjà.");
            }

            if (emailNormalise is not null && await _context.Employes.AnyAsync(e => e.Email != null && e.Email.ToLower() == emailNormalise))
            {
                ModelState.AddModelError(nameof(model.Email), "Cet email est déjà utilisé par un autre employé.");
            }

            if (await _context.Employes.AnyAsync(e => e.Telephone == telephoneComplet))
            {
                ModelState.AddModelError(nameof(model.NumeroTelephone), "Ce numéro de téléphone est déjà utilisé par un autre employé.");
            }

            if (!ModelState.IsValid)
            {
                model.Departements = await _context.Departements.Where(d => d.EstActif).OrderBy(d => d.Nom).ToListAsync();
                model.ActifsDisponibles = await _context.Actifs.Where(a => a.Etat == EtatActif.Disponible).OrderBy(a => a.Nom).ToListAsync();
                model.Categories = await _context.CategoriesActifs.Where(c => c.EstActif).OrderBy(c => c.Nom).ToListAsync();
                model.Indicatifs = EcoTrack.Enums.Indicatifs.Liste;
                return View(model);
            }

            var employe = new Employe
            {
                Nom = model.Nom,
                Prenom = model.Prenom,
                Email = model.Email,
                Telephone = telephoneComplet,
                Poste = model.Poste,
                DepartementId = model.DepartementId,
                EstActif = true
            };
            _context.Employes.Add(employe);
            await _context.SaveChangesAsync();

            Actif actif;
            if (model.ModeAttribution == ModeAttributionActif.ActifExistant)
            {
                actif = (await _context.Actifs.FindAsync(model.ActifExistantId!.Value))!;
            }
            else
            {
                actif = new Actif
                {
                    Nom = model.NouvelActifNom!,
                    NumeroSerie = model.NouvelActifNumeroSerie!,
                    Marque = model.NouvelActifMarque,
                    Modele = model.NouvelActifModele,
                    CategorieActifId = model.NouvelActifCategorieId!.Value,
                    DateAcquisition = DateTime.UtcNow.Date,
                    Etat = EtatActif.Disponible
                };
                _context.Actifs.Add(actif);
                await _context.SaveChangesAsync();
            }

            actif.Etat = EtatActif.Attribue;
            _context.Affectations.Add(new Affectation
            {
                ActifId = actif.Id,
                EmployeId = employe.Id,
                DateAffectation = DateTime.UtcNow,
                Motif = "Attribution à la création de l'employé"
            });

            await _context.SaveChangesAsync();

            TempData["Succes"] = $"L'employé \"{employe.Prenom} {employe.Nom}\" a été créé et l'actif \"{actif.Nom}\" lui a été attribué.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Employes/Edit/5
        [Authorize(Roles = "AdminPrincipal")]
        public async Task<IActionResult> Edit(int id)
        {
            var employe = await _context.Employes.FindAsync(id);
            if (employe is null)
            {
                return NotFound();
            }

            var (indicatif, numero) = DecomposerTelephone(employe.Telephone);

            var viewModel = new EmployeModifierViewModel
            {
                Id = employe.Id,
                Nom = employe.Nom,
                Prenom = employe.Prenom,
                Email = employe.Email,
                Indicatif = indicatif,
                NumeroTelephone = numero,
                Poste = employe.Poste,
                DepartementId = employe.DepartementId,
                Departements = await _context.Departements.Where(d => d.EstActif).OrderBy(d => d.Nom).ToListAsync()
            };

            return View(viewModel);
        }

        private static (string Indicatif, string Numero) DecomposerTelephone(string? telephone)
        {
            if (string.IsNullOrWhiteSpace(telephone))
            {
                return ("+228", string.Empty);
            }

            var parties = telephone.Split(' ', 2);
            return parties.Length == 2 ? (parties[0], parties[1]) : ("+228", telephone);
        }

        // POST /Employes/Edit/5
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployeModifierViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var telephoneComplet = $"{model.Indicatif} {model.NumeroTelephone}".Trim();
            var emailNormalise = model.Email?.Trim().ToLower();

            if (emailNormalise is not null && await _context.Employes.AnyAsync(e => e.Id != model.Id && e.Email != null && e.Email.ToLower() == emailNormalise))
            {
                ModelState.AddModelError(nameof(model.Email), "Cet email est déjà utilisé par un autre employé.");
            }

            if (await _context.Employes.AnyAsync(e => e.Id != model.Id && e.Telephone == telephoneComplet))
            {
                ModelState.AddModelError(nameof(model.NumeroTelephone), "Ce numéro de téléphone est déjà utilisé par un autre employé.");
            }

            if (!ModelState.IsValid)
            {
                model.Departements = await _context.Departements.Where(d => d.EstActif).OrderBy(d => d.Nom).ToListAsync();
                model.Indicatifs = EcoTrack.Enums.Indicatifs.Liste;
                return View(model);
            }

            var employe = await _context.Employes.FindAsync(id);
            if (employe is null)
            {
                return NotFound();
            }

            employe.Nom = model.Nom;
            employe.Prenom = model.Prenom;
            employe.Email = model.Email;
            employe.Telephone = telephoneComplet;
            employe.Poste = model.Poste;
            employe.DepartementId = model.DepartementId;

            await _context.SaveChangesAsync();

            TempData["Succes"] = $"Les informations de \"{employe.Prenom} {employe.Nom}\" ont été mises à jour. Ses actifs attribués n'ont pas été modifiés.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Employes/BasculerActivation/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BasculerActivation(int id)
        {
            var employe = await _context.Employes.FindAsync(id);
            if (employe is null)
            {
                return NotFound();
            }

            employe.EstActif = !employe.EstActif;
            await _context.SaveChangesAsync();

            TempData["Succes"] = employe.EstActif
                ? $"\"{employe.Prenom} {employe.Nom}\" a été réactivé."
                : $"\"{employe.Prenom} {employe.Nom}\" a été marqué comme inactif (ses actifs restent attribués tant qu'ils ne sont pas retirés manuellement).";

            return RedirectToAction(nameof(Index));
        }

        // GET /Employes/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var employe = await _context.Employes
                .Include(e => e.Departement)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employe is null)
            {
                return NotFound();
            }

            var affectationsActives = await _context.Affectations
                .Include(a => a.Actif)
                    .ThenInclude(a => a!.CategorieActif)
                .Where(a => a.EmployeId == id && a.DateRetrait == null)
                .OrderByDescending(a => a.DateAffectation)
                .ToListAsync();

            var historiqueComplet = await _context.Affectations
                .Include(a => a.Actif)
                .Where(a => a.EmployeId == id && a.DateRetrait != null)
                .OrderByDescending(a => a.DateRetrait)
                .ToListAsync();

            var viewModel = new EmployeDetailsViewModel
            {
                Employe = employe,
                AffectationsActives = affectationsActives,
                ActifsDisponibles = await _context.Actifs.Where(a => a.Etat == EtatActif.Disponible).OrderBy(a => a.Nom).ToListAsync(),
                HistoriqueComplet = historiqueComplet
            };

            return View(viewModel);
        }

        // POST /Employes/RetirerActif
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetirerActif(int affectationId, int employeId)
        {
            var affectation = await _context.Affectations
                .Include(a => a.Actif)
                .FirstOrDefaultAsync(a => a.Id == affectationId);

            if (affectation is null)
            {
                return NotFound();
            }

            affectation.DateRetrait = DateTime.UtcNow;
            if (affectation.Actif is not null)
            {
                affectation.Actif.Etat = EtatActif.Disponible;
            }

            await _context.SaveChangesAsync();

            TempData["Succes"] = $"L'actif \"{affectation.Actif?.Nom}\" a été retiré et repasse disponible.";
            return RedirectToAction(nameof(Details), new { id = employeId });
        }

        // POST /Employes/AttribuerActif
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AttribuerActif(int employeId, int actifId)
        {
            var actif = await _context.Actifs.FindAsync(actifId);
            var employe = await _context.Employes.FindAsync(employeId);

            if (actif is null || employe is null)
            {
                return NotFound();
            }

            if (actif.Etat != EtatActif.Disponible)
            {
                TempData["Erreur"] = "Cet actif n'est plus disponible.";
                return RedirectToAction(nameof(Details), new { id = employeId });
            }

            actif.Etat = EtatActif.Attribue;
            _context.Affectations.Add(new Affectation
            {
                ActifId = actif.Id,
                EmployeId = employe.Id,
                DateAffectation = DateTime.UtcNow,
                Motif = "Attribution manuelle"
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Erreur"] = "Cet actif vient d'être attribué par une autre action. Veuillez rafraîchir la page.";
                return RedirectToAction(nameof(Details), new { id = employeId });
            }

            TempData["Succes"] = $"L'actif \"{actif.Nom}\" a été attribué à {employe.Prenom} {employe.Nom}.";
            return RedirectToAction(nameof(Details), new { id = employeId });
        }
    }
}