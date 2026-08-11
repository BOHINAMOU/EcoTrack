using EcoTrack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EcoTrack.Controllers
{
    public partial class RapportsController
    {
        // GET /Rapports/Departement
        [HttpGet]
        public async Task<IActionResult> Departement()
        {
            ViewBag.Agences = await _context.Agences.Where(a => a.EstActif).OrderBy(a => a.Nom).ToListAsync();
            ViewBag.Departements = await _context.Departements.Where(d => d.EstActif).OrderBy(d => d.Nom).ToListAsync();
            ViewBag.Divisions = await _context.Divisions.Where(d => d.EstActif).OrderBy(d => d.Nom).ToListAsync();
            ViewBag.Services = await _context.Services.Where(s => s.EstActif).OrderBy(s => s.Nom).ToListAsync();
            ViewBag.Unites = await _context.Unites.Where(u => u.EstActif).OrderBy(u => u.Nom).ToListAsync();

            return View();
        }

        /// <summary>
        /// Résout "AG:3" / "DP:5" / "DV:2" / "SV:9" / "UN:4" en (titre, employés de cette unité et de ses sous-unités,
        /// actifs partagés directement rattachés à cette unité ou à l'une de ses sous-unités).
        /// </summary>
        private async Task<(string Titre, List<Employe> Employes, List<Actif> ActifsPartages)> ResoudreCibleRapport(string cible)
        {
            var parties = cible.Split(':');
            var type = parties.Length == 2 ? parties[0] : null;
            var id = parties.Length == 2 && int.TryParse(parties[1], out var idParse) ? idParse : (int?)null;

            if (type is null || !id.HasValue)
            {
                return (string.Empty, new List<Employe>(), new List<Actif>());
            }

            switch (type)
            {
                case "AG":
                    {
                        var agence = await _context.Agences.FindAsync(id.Value);
                        var employes = await _context.Employes
                            .Where(e => e.Unite!.Service!.Division!.Departement!.AgenceId == id.Value)
                            .ToListAsync();
                        var actifsPartages = await _context.Actifs
                            .Include(a => a.CategorieActif)
                            .Where(a =>
                                a.AgenceId == id.Value ||
                                (a.Departement != null && a.Departement.AgenceId == id.Value) ||
                                (a.Division != null && a.Division.Departement!.AgenceId == id.Value) ||
                                (a.Service != null && a.Service.Division!.Departement!.AgenceId == id.Value) ||
                                (a.Unite != null && a.Unite.Service!.Division!.Departement!.AgenceId == id.Value))
                            .ToListAsync();
                        return ($"Agence {agence?.Nom}", employes, actifsPartages);
                    }
                case "DP":
                    {
                        var departement = await _context.Departements.FindAsync(id.Value);
                        var employes = await _context.Employes
                            .Where(e => e.Unite!.Service!.Division!.DepartementId == id.Value)
                            .ToListAsync();
                        var actifsPartages = await _context.Actifs
                            .Include(a => a.CategorieActif)
                            .Where(a =>
                                a.DepartementId == id.Value ||
                                (a.Division != null && a.Division.DepartementId == id.Value) ||
                                (a.Service != null && a.Service.Division!.DepartementId == id.Value) ||
                                (a.Unite != null && a.Unite.Service!.Division!.DepartementId == id.Value))
                            .ToListAsync();
                        return ($"Département {departement?.Nom}", employes, actifsPartages);
                    }
                case "DV":
                    {
                        var division = await _context.Divisions.FindAsync(id.Value);
                        var employes = await _context.Employes
                            .Where(e => e.Unite!.Service!.DivisionId == id.Value)
                            .ToListAsync();
                        var actifsPartages = await _context.Actifs
                            .Include(a => a.CategorieActif)
                            .Where(a =>
                                a.DivisionId == id.Value ||
                                (a.Service != null && a.Service.DivisionId == id.Value) ||
                                (a.Unite != null && a.Unite.Service!.DivisionId == id.Value))
                            .ToListAsync();
                        return ($"Division {division?.Nom}", employes, actifsPartages);
                    }
                case "SV":
                    {
                        var service = await _context.Services.FindAsync(id.Value);
                        var employes = await _context.Employes
                            .Where(e => e.Unite!.ServiceId == id.Value)
                            .ToListAsync();
                        var actifsPartages = await _context.Actifs
                            .Include(a => a.CategorieActif)
                            .Where(a =>
                                a.ServiceId == id.Value ||
                                (a.Unite != null && a.Unite.ServiceId == id.Value))
                            .ToListAsync();
                        return ($"Service {service?.Nom}", employes, actifsPartages);
                    }
                case "UN":
                    {
                        var unite = await _context.Unites.FindAsync(id.Value);
                        var employes = await _context.Employes
                            .Where(e => e.UniteId == id.Value)
                            .ToListAsync();
                        var actifsPartages = await _context.Actifs
                            .Include(a => a.CategorieActif)
                            .Where(a => a.UniteId == id.Value)
                            .ToListAsync();
                        return ($"Unité {unite?.Nom}", employes, actifsPartages);
                    }
                default:
                    return (string.Empty, new List<Employe>(), new List<Actif>());
            }
        }

        // POST /Rapports/Departement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Departement(string? cible)
        {
            if (string.IsNullOrWhiteSpace(cible))
            {
                TempData["Erreur"] = "Choisissez une agence, un département, une division, un service ou une unité.";
                return RedirectToAction(nameof(Departement));
            }

            var (titre, employes, actifsPartages) = await ResoudreCibleRapport(cible);

            if (string.IsNullOrEmpty(titre))
            {
                TempData["Erreur"] = "Sélection invalide.";
                return RedirectToAction(nameof(Departement));
            }

            var employeIds = employes.Select(e => e.Id).ToList();

            var affectationsActives = await _context.Affectations
                .Include(a => a.Actif)
                    .ThenInclude(a => a!.CategorieActif)
                .Include(a => a.Employe)
                .Where(a => employeIds.Contains(a.EmployeId) && a.DateRetrait == null)
                .ToListAsync();

            var document = new RapportDepartementDocument(titre, employes, affectationsActives, actifsPartages);
            var pdf = document.GeneratePdf();

            return File(pdf, "application/pdf", $"Rapport_{titre.Replace(" ", "_")}.pdf");
        }
    }

    public class RapportDepartementDocument : IDocument
    {
        private readonly string _titre;
        private readonly List<Employe> _employes;
        private readonly List<Affectation> _affectationsActives;
        private readonly List<Actif> _actifsPartages;

        public RapportDepartementDocument(string titre, List<Employe> employes, List<Affectation> affectationsActives, List<Actif> actifsPartages)
        {
            _titre = titre;
            _employes = employes;
            _affectationsActives = affectationsActives;
            _actifsPartages = actifsPartages;
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("EcoTrack — Ecobank Togo").FontSize(16).Bold().FontColor("#0d3b66");
                    col.Item().Text($"Rapport — {_titre}").FontSize(11).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor("#0d3b66");
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Spacing(15);

                    col.Item().Text($"{_employes.Count} employé(s) — {_affectationsActives.Count} actif(s) attribué(s) individuellement — {_actifsPartages.Count} actif(s) partagé(s)")
                        .FontSize(11).Bold();

                    col.Item().Text("Employés et leurs actifs").Bold().FontSize(12);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                            c.RelativeColumn(4);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Employé").Bold();
                            header.Cell().Text("Poste").Bold();
                            header.Cell().Text("Actifs attribués").Bold();
                        });

                        foreach (var employe in _employes.OrderBy(e => e.Nom))
                        {
                            var actifsEmploye = _affectationsActives
                                .Where(a => a.EmployeId == employe.Id)
                                .Select(a => a.Actif?.Nom)
                                .Where(n => n is not null);

                            table.Cell().Text($"{employe.Prenom} {employe.Nom}");
                            table.Cell().Text(employe.Poste ?? "—");
                            table.Cell().Text(actifsEmploye.Any() ? string.Join(", ", actifsEmploye) : "Aucun actif");
                        }
                    });

                    if (!_employes.Any())
                    {
                        col.Item().Text("Aucun employé.").Italic().FontColor(Colors.Grey.Darken1);
                    }

                    col.Item().Text("Actifs partagés (non attribués à un employé précis)").Bold().FontSize(12);
                    if (_actifsPartages.Any())
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(3);
                                c.RelativeColumn(3);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Actif").Bold();
                                header.Cell().Text("N° de série").Bold();
                                header.Cell().Text("Catégorie").Bold();
                            });

                            foreach (var actif in _actifsPartages.OrderBy(a => a.Nom))
                            {
                                table.Cell().Text(actif.Nom);
                                table.Cell().Text(actif.NumeroSerie);
                                table.Cell().Text(actif.CategorieActif?.Nom ?? "—");
                            }
                        });
                    }
                    else
                    {
                        col.Item().Text("Aucun actif partagé.").Italic().FontColor(Colors.Grey.Darken1);
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Généré le ").FontSize(8);
                    x.Span(DateTime.UtcNow.ToString("dd/MM/yyyy à HH:mm")).FontSize(8);
                    x.Span(" — Document EcoTrack").FontSize(8);
                });
            });
        }
    }
}