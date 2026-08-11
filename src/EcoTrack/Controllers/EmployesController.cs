using EcoTrack.Data;
using EcoTrack.Enums;
using EcoTrack.Infrastructure;
using EcoTrack.Models;
using EcoTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcoTrack.Controllers
{
    [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
    public class EmployesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IJournalService _journal;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public EmployesController(ApplicationDbContext context, IJournalService journal, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _context = context;
            _journal = journal;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        private string UtilisateurConnecteId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        private async Task ChargerListesOrganisation(dynamic model)
        {
            model.Agences = await _context.Agences.Where(a => a.EstActif).OrderBy(a => a.Nom).ToListAsync();
            model.Departements = await _context.Departements.Where(d => d.EstActif).OrderBy(d => d.Nom).ToListAsync();
            model.Divisions = await _context.Divisions.Where(d => d.EstActif).OrderBy(d => d.Nom).ToListAsync();
            model.Services = await _context.Services.Where(s => s.EstActif).OrderBy(s => s.Nom).ToListAsync();
            model.Unites = await _context.Unites.Where(u => u.EstActif).OrderBy(u => u.Nom).ToListAsync();
        }

        // GET /Employes
        public async Task<IActionResult> Index()
        {
            var employes = await _context.Employes
                .Include(e => e.Unite!)
                    .ThenInclude(u => u.Service!)
                        .ThenInclude(s => s.Division!)
                            .ThenInclude(d => d.Departement!)
                                .ThenInclude(dep => dep.Agence)
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
                .Where(e => (e.Prenom + " " + e.Nom).ToLower().Contains(termeNormalise)
                         || (e.Nom + " " + e.Prenom).ToLower().Contains(termeNormalise)
                         || (e.Poste != null && e.Poste.ToLower().Contains(termeNormalise)))
                .OrderBy(e => e.Nom)
                .Take(20)
                .Select(e => new
                {
                    id = e.Id,
                    nomComplet = e.Prenom + " " + e.Nom,
                    departement = e.Unite != null && e.Unite.Service != null && e.Unite.Service.Division != null && e.Unite.Service.Division.Departement != null
                        ? e.Unite.Service.Division.Departement.Agence!.Nom
                        : "—",
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
                ActifsDisponibles = await _context.Actifs.Where(a => a.Etat == EtatActif.Disponible).OrderBy(a => a.Nom).ToListAsync(),
                Categories = await _context.CategoriesActifs.Where(c => c.EstActif).OrderBy(c => c.Nom).ToListAsync()
            };
            await ChargerListesOrganisation(viewModel);

            return View(viewModel);
        }

        // POST /Employes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeCreerViewModel model)
        {
            var nomNormalise = (model.Nom ?? string.Empty).Trim().ToLower();
            var prenomNormalise = (model.Prenom ?? string.Empty).Trim().ToLower();
            var emailNormalise = (model.Email ?? string.Empty).Trim().ToLower();
            var telephoneComplet = $"{model.Indicatif} {model.NumeroTelephone}".Trim();

            var emailsExistants = await _context.Employes
                .Where(e => e.Nom.ToLower() == nomNormalise && e.Prenom.ToLower() == prenomNormalise)
                .Select(e => e.Email)
                .ToListAsync();

            if (emailsExistants.Any(e => e.ToLower() == emailNormalise))
            {
                ModelState.AddModelError(string.Empty, "Un employé avec ce nom, ce prénom et cet email existe déjà.");
            }

            if (await _context.Employes.AnyAsync(e => e.Email.ToLower() == emailNormalise))
            {
                ModelState.AddModelError(nameof(model.Email), "Cet email est déjà utilisé par un autre employé.");
            }

            if (await _context.Employes.AnyAsync(e => e.Telephone == telephoneComplet))
            {
                ModelState.AddModelError(nameof(model.NumeroTelephone), "Ce numéro de téléphone est déjà utilisé par un autre employé.");
            }

            var uniteSelectionnee = await _context.Unites.FindAsync(model.UniteId);
            if (uniteSelectionnee is null)
            {
                ModelState.AddModelError(nameof(model.UniteId), "L'unité sélectionnée est invalide.");
            }

            if (model.ModeAttribution == ModeAttributionActif.ActifExistant)
            {
                if (model.ActifExistantId is null)
                {
                    ModelState.AddModelError(nameof(model.ActifExistantId), "Veuillez sélectionner un actif à attribuer.");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.NouvelActifNom))
                {
                    ModelState.AddModelError(nameof(model.NouvelActifNom), "Le nom du nouvel actif est obligatoire.");
                }
                if (string.IsNullOrWhiteSpace(model.NouvelActifNumeroSerie))
                {
                    ModelState.AddModelError(nameof(model.NouvelActifNumeroSerie), "Le numéro de série est obligatoire.");
                }
                else if (await _context.Actifs.AnyAsync(a => a.NumeroSerie.ToLower() == model.NouvelActifNumeroSerie.ToLower()))
                {
                    ModelState.AddModelError(nameof(model.NouvelActifNumeroSerie), "Ce numéro de série existe déjà.");
                }
                if (model.NouvelActifCategorieId is null)
                {
                    ModelState.AddModelError(nameof(model.NouvelActifCategorieId), "La catégorie du nouvel actif est obligatoire.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.ActifsDisponibles = await _context.Actifs.Where(a => a.Etat == EtatActif.Disponible).OrderBy(a => a.Nom).ToListAsync();
                model.Categories = await _context.CategoriesActifs.Where(c => c.EstActif).OrderBy(c => c.Nom).ToListAsync();
                model.IndicatifsListe = EcoTrack.Enums.Indicatifs.Liste;
                await ChargerListesOrganisation(model);
                return View(model);
            }

            var employe = new Employe
            {
                Nom = model.Nom,
                Prenom = model.Prenom,
                Email = model.Email,
                Telephone = telephoneComplet,
                Poste = model.Poste,
                UniteId = model.UniteId,
                ServiceId = uniteSelectionnee!.ServiceId,
                EstActif = true
            };
            _context.Employes.Add(employe);
            await _context.SaveChangesAsync();

            var resultatCompte = await GestionComptesEmployes.CreerCompteSiAbsentAsync(employe, _context, _userManager, _emailSender, model.NomUtilisateur);
            if (!resultatCompte.Succes)
            {
                TempData["Erreur"] = $"L'employé a été créé, mais la création de son compte de connexion a échoué : {resultatCompte.MessageErreur}";
            }
            else if (resultatCompte.MessageErreur == "email_non_envoye")
            {
                TempData["Erreur"] = $"L'employé a été créé, mais l'email avec ses identifiants n'a pas pu être envoyé. Nom d'utilisateur : {resultatCompte.NomUtilisateurGenere} — Mot de passe temporaire : {resultatCompte.MotDePasseGenere} (à communiquer manuellement).";
            }

            Actif actif;
            bool estNouvelActif = model.ModeAttribution == ModeAttributionActif.NouvelActif;

            if (!estNouvelActif)
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

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "CreationEmploye",
                $"A créé l'employé \"{employe.Prenom} {employe.Nom}\" et lui a attribué l'actif \"{actif.Nom}\"" + (estNouvelActif ? " (nouvel actif enregistré)" : ""));

            TempData["Succes"] = $"L'employé \"{employe.Prenom} {employe.Nom}\" a été créé et l'actif \"{actif.Nom}\" lui a été attribué.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Employes/Edit/5
        [Authorize(Roles = "AdminPrincipal")]
        public async Task<IActionResult> Edit(int id)
        {
            var employe = await _context.Employes
                .Include(e => e.Unite!)
                    .ThenInclude(u => u.Service!)
                        .ThenInclude(s => s.Division!)
                            .ThenInclude(d => d.Departement)
                .FirstOrDefaultAsync(e => e.Id == id);

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
                UniteId = employe.UniteId
            };
            await ChargerListesOrganisation(viewModel);

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
            var emailNormalise = (model.Email ?? string.Empty).Trim().ToLower();

            if (await _context.Employes.AnyAsync(e => e.Id != model.Id && e.Email.ToLower() == emailNormalise))
            {
                ModelState.AddModelError(nameof(model.Email), "Cet email est déjà utilisé par un autre employé.");
            }

            if (await _context.Employes.AnyAsync(e => e.Id != model.Id && e.Telephone == telephoneComplet))
            {
                ModelState.AddModelError(nameof(model.NumeroTelephone), "Ce numéro de téléphone est déjà utilisé par un autre employé.");
            }

            var uniteSelectionnee = await _context.Unites.FindAsync(model.UniteId);
            if (uniteSelectionnee is null)
            {
                ModelState.AddModelError(nameof(model.UniteId), "L'unité sélectionnée est invalide.");
            }

            if (!ModelState.IsValid)
            {
                model.IndicatifsListe = EcoTrack.Enums.Indicatifs.Liste;
                await ChargerListesOrganisation(model);
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
            employe.UniteId = model.UniteId;
            employe.ServiceId = uniteSelectionnee!.ServiceId;

            await _context.SaveChangesAsync();

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "ModificationEmploye",
                $"A modifié les informations de \"{employe.Prenom} {employe.Nom}\"");

            TempData["Succes"] = $"Les informations de \"{employe.Prenom} {employe.Nom}\" ont été mises à jour. Ses actifs attribués n'ont pas été modifiés.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Employes/BasculerActivation/5
        [HttpPost]
        [Authorize(Roles = "AdminPrincipal")]
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

            await _journal.EnregistrerAsync(UtilisateurConnecteId, employe.EstActif ? "ReactivationEmploye" : "DesactivationEmploye",
                $"A {(employe.EstActif ? "réactivé" : "désactivé")} l'employé \"{employe.Prenom} {employe.Nom}\"");

            TempData["Succes"] = employe.EstActif
                ? $"\"{employe.Prenom} {employe.Nom}\" a été réactivé."
                : $"\"{employe.Prenom} {employe.Nom}\" a été marqué comme inactif (ses actifs restent attribués tant qu'ils ne sont pas retirés manuellement).";

            return RedirectToAction(nameof(Index));
        }

        // GET /Employes/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var employe = await _context.Employes
                .Include(e => e.Unite!)
                    .ThenInclude(u => u.Service!)
                        .ThenInclude(s => s.Division!)
                            .ThenInclude(d => d.Departement!)
                                .ThenInclude(dep => dep.Agence)
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

            var actifIdsConcernes = affectationsActives.Select(a => a.ActifId)
                .Concat(historiqueComplet.Select(a => a.ActifId))
                .Distinct()
                .ToList();

            var toutesLesAffectations = await _context.Affectations
                .Include(a => a.Employe)
                .Where(a => actifIdsConcernes.Contains(a.ActifId))
                .OrderBy(a => a.ActifId).ThenBy(a => a.DateAffectation)
                .ToListAsync();

            List<HistoriqueLigneViewModel> ConstruireLignes(List<Affectation> affectations) => affectations.Select(aff =>
            {
                var chronologieActif = toutesLesAffectations.Where(x => x.ActifId == aff.ActifId).ToList();
                var index = chronologieActif.FindIndex(x => x.Id == aff.Id);
                var precedente = index > 0 ? chronologieActif[index - 1] : null;

                return new HistoriqueLigneViewModel
                {
                    Affectation = aff,
                    EstReattribution = precedente is not null,
                    DetenteurPrecedentNom = precedente is not null ? $"{precedente.Employe?.Prenom} {precedente.Employe?.Nom}" : null,
                    MemeEmployeQuAvant = precedente is not null && precedente.EmployeId == aff.EmployeId
                };
            }).ToList();

            var viewModel = new EmployeDetailsViewModel
            {
                Employe = employe,
                AffectationsActives = ConstruireLignes(affectationsActives),
                ActifsDisponibles = await _context.Actifs.Where(a => a.Etat == EtatActif.Disponible).OrderBy(a => a.Nom).ToListAsync(),
                HistoriqueComplet = ConstruireLignes(historiqueComplet)
            };

            return View(viewModel);
        }

        // POST /Employes/RetirerActif
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetirerActif(int affectationId, int employeId, string? motif)
        {
            var affectation = await _context.Affectations
                .Include(a => a.Actif)
                .Include(a => a.Employe)
                .FirstOrDefaultAsync(a => a.Id == affectationId);

            if (affectation is null)
            {
                return NotFound();
            }

            affectation.DateRetrait = DateTime.UtcNow;
            affectation.Motif = string.IsNullOrWhiteSpace(motif) ? "Retiré sans motif précisé" : motif.Trim();

            if (affectation.Actif is not null)
            {
                affectation.Actif.Etat = EtatActif.Disponible;
            }

            await _context.SaveChangesAsync();

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "RetraitActif",
                $"A retiré l'actif \"{affectation.Actif?.Nom}\" de \"{affectation.Employe?.Prenom} {affectation.Employe?.Nom}\" (motif : {affectation.Motif})");

            TempData["Succes"] = $"L'actif \"{affectation.Actif?.Nom}\" a été retiré et repasse disponible.";
            return RedirectToAction(nameof(Details), new { id = employeId });
        }

        // POST /Employes/AttribuerActif
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AttribuerActif(int employeId, int actifId, string? motif)
        {
            var actif = await _context.Actifs.FindAsync(actifId);
            var employe = await _context.Employes.FindAsync(employeId);

            if (actif is null || employe is null)
            {
                return NotFound();
            }

            if (!employe.EstActif)
            {
                TempData["Erreur"] = "Impossible d'attribuer un actif à un employé désactivé. Réactivez-le d'abord.";
                return RedirectToAction(nameof(Details), new { id = employeId });
            }

            if (actif.Etat != EtatActif.Disponible)
            {
                TempData["Erreur"] = "Cet actif n'est plus disponible.";
                return RedirectToAction(nameof(Details), new { id = employeId });
            }

            var motifFinal = string.IsNullOrWhiteSpace(motif) ? "Attribution manuelle" : motif.Trim();

            actif.Etat = EtatActif.Attribue;
            _context.Affectations.Add(new Affectation
            {
                ActifId = actif.Id,
                EmployeId = employe.Id,
                DateAffectation = DateTime.UtcNow,
                Motif = motifFinal
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

            await _journal.EnregistrerAsync(UtilisateurConnecteId, "AttributionActif",
                $"A attribué l'actif \"{actif.Nom}\" à \"{employe.Prenom} {employe.Nom}\" (motif : {motifFinal})");

            TempData["Succes"] = $"L'actif \"{actif.Nom}\" a été attribué à {employe.Prenom} {employe.Nom}.";
            return RedirectToAction(nameof(Details), new { id = employeId });
        }
    }
}