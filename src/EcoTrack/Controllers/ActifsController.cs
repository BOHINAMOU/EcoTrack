using EcoTrack.Data;
using EcoTrack.Enums;
using EcoTrack.Infrastructure;
using EcoTrack.Models;
using EcoTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace EcoTrack.Controllers
{
    [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
    public class ActifsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IJournalService _journal;

        public ActifsController(ApplicationDbContext context, IJournalService journal)
        {
            _context = context;
            _journal = journal;
        }

        private string UtilisateurConnecteId => User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? string.Empty;

        private async Task ChargerListesUnites(ActifFormViewModel model)
        {
            model.Employes = await _context.Employes.Where(e => e.EstActif).OrderBy(e => e.Nom).ToListAsync();
            model.Agences = await _context.Agences.Where(a => a.EstActif).OrderBy(a => a.Nom).ToListAsync();
            model.Departements = await _context.Departements.Where(d => d.EstActif).OrderBy(d => d.Nom).ToListAsync();
            model.Divisions = await _context.Divisions.Where(d => d.EstActif).OrderBy(d => d.Nom).ToListAsync();
            model.Services = await _context.Services.Where(s => s.EstActif).OrderBy(s => s.Nom).ToListAsync();
            model.Unites = await _context.Unites.Where(u => u.EstActif).OrderBy(u => u.Nom).ToListAsync();
        }

        /// <summary>
        /// Applique le choix "EM:12" (employé) / "AG:3" / "DP:5" / "DV:2" / "SV:9" (unité) / vide (disponible)
        /// sur l'actif. Gère aussi l'ouverture/fermeture de l'affectation employé le cas échéant.
        /// Retourne l'état résultant (Attribue ou Disponible).
        /// </summary>
        private async Task<EtatActif> AppliquerAttribution(Actif actif, string? attribution)
        {
            actif.AgenceId = null;
            actif.DepartementId = null;
            actif.DivisionId = null;
            actif.ServiceId = null;
            actif.UniteId = null;

            var affectationActive = actif.Id != 0
                ? await _context.Affectations.FirstOrDefaultAsync(a => a.ActifId == actif.Id && a.DateRetrait == null)
                : null;

            var parties = (attribution ?? string.Empty).Split(':');
            var type = parties.Length == 2 ? parties[0] : null;
            var idValide = parties.Length == 2 && int.TryParse(parties[1], out var idParse) ? idParse : (int?)null;

            if (type == "EM" && idValide.HasValue)
            {
                if (affectationActive is not null && affectationActive.EmployeId == idValide.Value)
                {
                    // Déjà attribué à ce même employé : rien à changer.
                    return EtatActif.Attribue;
                }

                if (affectationActive is not null)
                {
                    affectationActive.DateRetrait = DateTime.UtcNow;
                    affectationActive.Motif = "Réattribué à un autre employé";
                }

                _context.Affectations.Add(new Affectation
                {
                    ActifId = actif.Id,
                    EmployeId = idValide.Value,
                    DateAffectation = DateTime.UtcNow
                });

                return EtatActif.Attribue;
            }

            // Toute autre option (unité, ou aucun) ferme l'affectation employé en cours s'il y en a une.
            if (affectationActive is not null)
            {
                affectationActive.DateRetrait = DateTime.UtcNow;
                affectationActive.Motif = "Retiré : réattribution à une unité ou remise disponible";
            }

            switch (type)
            {
                case "AG" when idValide.HasValue: actif.AgenceId = idValide; return EtatActif.Attribue;
                case "DP" when idValide.HasValue: actif.DepartementId = idValide; return EtatActif.Attribue;
                case "DV" when idValide.HasValue: actif.DivisionId = idValide; return EtatActif.Attribue;
                case "SV" when idValide.HasValue: actif.ServiceId = idValide; return EtatActif.Attribue;
                case "UN" when idValide.HasValue: actif.UniteId = idValide; return EtatActif.Attribue;
                default: return EtatActif.Disponible;
            }
        }

        // GET /Actifs?etat=Disponible
        public async Task<IActionResult> Index(string? etat)
        {
            var requete = _context.Actifs
                .Include(a => a.CategorieActif)
                .Include(a => a.Affectations.Where(aff => aff.DateRetrait == null))
                    .ThenInclude(aff => aff.Employe)
                .Include(a => a.Agence)
                .Include(a => a.Departement)
                .Include(a => a.Division)
                .Include(a => a.Service)
                .Include(a => a.Unite)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(etat) && Enum.TryParse<EtatActif>(etat, true, out var etatFiltre))
            {
                requete = requete.Where(a => a.Etat == etatFiltre);
            }

            var actifs = await requete.OrderBy(a => a.Nom).ToListAsync();

            ViewBag.FiltreActuel = etat;
            return View(actifs);
        }

        // GET /Actifs/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var actif = await _context.Actifs
                .Include(a => a.CategorieActif)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (actif is null)
            {
                return NotFound();
            }

            var historique = await _context.Affectations
                .Include(af => af.Employe)
                .Where(af => af.ActifId == id)
                .OrderBy(af => af.DateAffectation)
                .ToListAsync();

            var lignes = historique.Select((aff, index) =>
            {
                var precedente = index > 0 ? historique[index - 1] : null;

                return new HistoriqueLigneViewModel
                {
                    Affectation = aff,
                    EstReattribution = precedente is not null,
                    DetenteurPrecedentNom = precedente is not null ? $"{precedente.Employe?.Prenom} {precedente.Employe?.Nom}" : null,
                    MemeEmployeQuAvant = precedente is not null && precedente.EmployeId == aff.EmployeId
                };
            })
            .OrderByDescending(l => l.Affectation.DateAffectation)
            .ToList();

            ViewBag.Historique = lignes;
            return View(actif);
        }

        // GET /Actifs/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new ActifFormViewModel
            {
                Categories = await _context.CategoriesActifs.Where(c => c.EstActif).OrderBy(c => c.Nom).ToListAsync()
            };
            await ChargerListesUnites(viewModel);

            return View(viewModel);
        }

        // POST /Actifs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActifFormViewModel model)
        {
            var numeroSerieNormalise = (model.NumeroSerie ?? string.Empty).Trim().ToLower();

            var existeDeja = !string.IsNullOrWhiteSpace(numeroSerieNormalise)
                && await _context.Actifs.AnyAsync(a => a.NumeroSerie.ToLower() == numeroSerieNormalise);

            if (existeDeja)
            {
                ModelState.AddModelError(nameof(model.NumeroSerie), "Ce numéro de série existe déjà.");
            }

            if (!ModelState.IsValid)
            {
                model.Categories = await _context.CategoriesActifs.Where(c => c.EstActif).OrderBy(c => c.Nom).ToListAsync();
                await ChargerListesUnites(model);
                return View(model);
            }

            var actif = new Actif
            {
                Nom = model.Nom,
                NumeroSerie = model.NumeroSerie,
                Marque = model.Marque,
                Modele = model.Modele,
                DateAcquisition = DateTime.SpecifyKind(model.DateAcquisition, DateTimeKind.Utc),
                CategorieActifId = model.CategorieActifId,
                Etat = EtatActif.Disponible
            };

            _context.Actifs.Add(actif);
            await _context.SaveChangesAsync();

            actif.Etat = await AppliquerAttribution(actif, model.ProprietaireUnite);
            await _context.SaveChangesAsync();

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "CreationActif",
                $"A enregistré un nouvel actif \"{actif.Nom}\" (N° série : {actif.NumeroSerie})");

            TempData["Succes"] = $"L'actif \"{actif.Nom}\" a été enregistré" + (actif.Etat == EtatActif.Attribue ? "." : " et est disponible.");
            return RedirectToAction(nameof(Index));
        }

        // GET /Actifs/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var actif = await _context.Actifs.FindAsync(id);
            if (actif is null)
            {
                return NotFound();
            }

            var affectationActive = await _context.Affectations
                .FirstOrDefaultAsync(a => a.ActifId == id && a.DateRetrait == null);

            var viewModel = new ActifFormViewModel
            {
                Id = actif.Id,
                Nom = actif.Nom,
                NumeroSerie = actif.NumeroSerie,
                Marque = actif.Marque,
                Modele = actif.Modele,
                DateAcquisition = actif.DateAcquisition,
                CategorieActifId = actif.CategorieActifId,
                Categories = await _context.CategoriesActifs.Where(c => c.EstActif).OrderBy(c => c.Nom).ToListAsync(),
                ProprietaireUnite = affectationActive is not null ? $"EM:{affectationActive.EmployeId}"
                    : actif.AgenceId is not null ? $"AG:{actif.AgenceId}"
                    : actif.DepartementId is not null ? $"DP:{actif.DepartementId}"
                    : actif.DivisionId is not null ? $"DV:{actif.DivisionId}"
                    : actif.ServiceId is not null ? $"SV:{actif.ServiceId}"
                    : actif.UniteId is not null ? $"UN:{actif.UniteId}"
                    : null
            };
            await ChargerListesUnites(viewModel);

            return View(viewModel);
        }

        // POST /Actifs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ActifFormViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var numeroSerieNormalise = (model.NumeroSerie ?? string.Empty).Trim().ToLower();

            var existeDeja = !string.IsNullOrWhiteSpace(numeroSerieNormalise)
                && await _context.Actifs.AnyAsync(a => a.Id != model.Id && a.NumeroSerie.ToLower() == numeroSerieNormalise);

            if (existeDeja)
            {
                ModelState.AddModelError(nameof(model.NumeroSerie), "Ce numéro de série existe déjà.");
            }

            if (!ModelState.IsValid)
            {
                model.Categories = await _context.CategoriesActifs.Where(c => c.EstActif).OrderBy(c => c.Nom).ToListAsync();
                await ChargerListesUnites(model);
                return View(model);
            }

            var actif = await _context.Actifs.FindAsync(id);
            if (actif is null)
            {
                return NotFound();
            }

            actif.Nom = model.Nom;
            actif.NumeroSerie = model.NumeroSerie;
            actif.Marque = model.Marque;
            actif.Modele = model.Modele;
            actif.DateAcquisition = DateTime.SpecifyKind(model.DateAcquisition, DateTimeKind.Utc);
            actif.CategorieActifId = model.CategorieActifId;

            // On ne touche à l'attribution que si l'actif n'est pas en panne ou détérioré —
            // pour ne jamais réattribuer silencieusement un actif hors service.
            if (actif.Etat != EtatActif.EnPanne && actif.Etat != EtatActif.Deteriore)
            {
                actif.Etat = await AppliquerAttribution(actif, model.ProprietaireUnite);
            }

            await _context.SaveChangesAsync();

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "ModificationActif",
                $"A modifié l'actif \"{actif.Nom}\" (N° série : {actif.NumeroSerie})");

            TempData["Succes"] = $"L'actif \"{actif.Nom}\" a été modifié.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Actifs/MarquerDeteriore/5
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarquerDeteriore(int id)
        {
            var actif = await _context.Actifs.FindAsync(id);
            if (actif is null)
            {
                return NotFound();
            }

            if (actif.Etat == EtatActif.Attribue)
            {
                TempData["Erreur"] = "Impossible de marquer cet actif comme détérioré : il est actuellement attribué à un employé. Retirez-le d'abord.";
                return RedirectToAction(nameof(Index));
            }

            actif.Etat = EtatActif.Deteriore;
            await _context.SaveChangesAsync();

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "DeteriorationActif",
                $"A marqué l'actif \"{actif.Nom}\" comme détérioré");

            TempData["Succes"] = $"L'actif \"{actif.Nom}\" a été marqué comme détérioré.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Actifs/Reactiver/5
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactiver(int id)
        {
            var actif = await _context.Actifs.FindAsync(id);
            if (actif is null)
            {
                return NotFound();
            }

            if (actif.Etat != EtatActif.Deteriore)
            {
                return RedirectToAction(nameof(Index));
            }

            actif.Etat = EtatActif.Disponible;
            await _context.SaveChangesAsync();

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "ReactivationActif",
                $"A remis l'actif \"{actif.Nom}\" disponible");

            TempData["Succes"] = $"L'actif \"{actif.Nom}\" a été remis disponible.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Actifs/MettreEnPanne/5
        // Retire l'actif de son détenteur actuel (s'il en a un) et le passe en panne.
        // L'affectation en cours est close normalement (DateRetrait renseignée), donc l'historique
        // "qui l'utilisait avant la panne" reste consultable pour le remettre au bon employé plus tard.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MettreEnPanne(int id, string? motif)
        {
            var actif = await _context.Actifs.FindAsync(id);
            if (actif is null)
            {
                return NotFound();
            }

            var affectationActive = await _context.Affectations
                .Where(a => a.ActifId == id && a.DateRetrait == null)
                .FirstOrDefaultAsync();

            if (affectationActive is not null)
            {
                affectationActive.DateRetrait = DateTime.UtcNow;
                affectationActive.Motif = string.IsNullOrWhiteSpace(motif) ? "Retiré : mise en panne" : motif;
            }

            actif.Etat = EtatActif.EnPanne;
            await _context.SaveChangesAsync();

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "MiseEnPanneActif",
                $"A mis l'actif \"{actif.Nom}\" en panne" + (affectationActive is not null ? " (retiré à son détenteur actuel)" : ""));

            TempData["Succes"] = $"L'actif \"{actif.Nom}\" a été mis en panne.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Actifs/SortirDePanne/5
        // L'actif réparé retourne automatiquement à qui l'utilisait juste avant la panne
        // (nouvelle affectation créée), ou repasse simplement "Disponible" s'il n'avait personne.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SortirDePanne(int id)
        {
            var actif = await _context.Actifs.FindAsync(id);
            if (actif is null)
            {
                return NotFound();
            }

            if (actif.Etat != EtatActif.EnPanne)
            {
                return RedirectToAction(nameof(Index));
            }

            var derniereAffectation = await _context.Affectations
                .Include(a => a.Employe)
                .Where(a => a.ActifId == id)
                .OrderByDescending(a => a.DateRetrait)
                .FirstOrDefaultAsync();

            if (derniereAffectation is not null && derniereAffectation.Employe is not null && derniereAffectation.Employe.EstActif)
            {
                _context.Affectations.Add(new Affectation
                {
                    ActifId = actif.Id,
                    EmployeId = derniereAffectation.EmployeId,
                    DateAffectation = DateTime.UtcNow,
                    Motif = "Réattribué après réparation"
                });

                actif.Etat = EtatActif.Attribue;

                await _context.SaveChangesAsync();

                await _journal.EnregistrerAsync(UtilisateurConnecteId, "SortieDePanneActif",
                    $"A sorti l'actif \"{actif.Nom}\" de panne et l'a réattribué à \"{derniereAffectation.Employe.Prenom} {derniereAffectation.Employe.Nom}\".");

                TempData["Succes"] = $"L'actif \"{actif.Nom}\" a été réparé et réattribué à {derniereAffectation.Employe.Prenom} {derniereAffectation.Employe.Nom}.";
            }
            else
            {
                actif.Etat = EtatActif.Disponible;
                await _context.SaveChangesAsync();

                await _journal.EnregistrerAsync(UtilisateurConnecteId, "SortieDePanneActif",
                    $"A sorti l'actif \"{actif.Nom}\" de panne (aucun détenteur précédent, remis disponible).");

                TempData["Succes"] = $"L'actif \"{actif.Nom}\" a été réparé et remis disponible.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}