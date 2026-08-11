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
        // GET /Rapports/Employe
        [HttpGet]
        public async Task<IActionResult> Employe()
        {
            ViewBag.Employes = await _context.Employes
                .OrderBy(e => e.Nom)
                .Select(e => new { e.Id, NomComplet = e.Prenom + " " + e.Nom })
                .ToListAsync();

            return View();
        }

        // POST /Rapports/Employe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Employe(int? employeId)
        {
            var employesRequete = _context.Employes
                .Include(e => e.Unite!)
                    .ThenInclude(u => u.Service!)
                        .ThenInclude(s => s.Division!)
                            .ThenInclude(d => d.Departement!)
                .Include(e => e.Service)
                .AsQueryable();

            if (employeId.HasValue)
            {
                employesRequete = employesRequete.Where(e => e.Id == employeId.Value);
            }

            var employes = await employesRequete.OrderBy(e => e.Nom).ToListAsync();

            if (!employes.Any())
            {
                TempData["Erreur"] = "Aucun employé trouvé.";
                return RedirectToAction(nameof(Employe));
            }

            var employeIds = employes.Select(e => e.Id).ToList();

            // Toutes les affectations de ces employés (actives + historique)
            var affectationsDesEmployes = await _context.Affectations
                .Include(a => a.Actif)
                    .ThenInclude(a => a!.CategorieActif)
                .Where(a => employeIds.Contains(a.EmployeId))
                .OrderByDescending(a => a.DateAffectation)
                .ToListAsync();

            // Pour chaque actif qui apparaît dans l'historique, on va chercher qui le détient
            // ACTUELLEMENT (peut être personne, l'employé lui-même, ou quelqu'un d'autre).
            var actifIdsConcernes = affectationsDesEmployes.Select(a => a.ActifId).Distinct().ToList();
            var detenteursActuels = await _context.Affectations
                .Include(a => a.Employe)
                .Where(a => actifIdsConcernes.Contains(a.ActifId) && a.DateRetrait == null)
                .ToDictionaryAsync(a => a.ActifId, a => a.Employe);

            var document = new RapportEmployeDocument(employes, affectationsDesEmployes, detenteursActuels);
            var pdf = document.GeneratePdf();

            var nomFichier = employeId.HasValue
                ? $"Rapport_Employe_{employes.First().Nom}.pdf"
                : "Rapport_Tous_Employes.pdf";

            return File(pdf, "application/pdf", nomFichier);
        }
    }

    public class RapportEmployeDocument : IDocument
    {
        private readonly List<Employe> _employes;
        private readonly List<Affectation> _affectations;
        private readonly Dictionary<int, Employe?> _detenteursActuels;

        public RapportEmployeDocument(List<Employe> employes, List<Affectation> affectations, Dictionary<int, Employe?> detenteursActuels)
        {
            _employes = employes;
            _affectations = affectations;
            _detenteursActuels = detenteursActuels;
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor("#1f2937"));

                page.Header().Background("#0d3b66").Padding(15).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("RAPPORT DES ACTIFS UTILISÉS").FontSize(16).Bold().FontColor(Colors.White);
                        col.Item().Text("EcoTrack — Ecobank Togo").FontSize(9).FontColor(Colors.Grey.Lighten3);
                    });
                    row.ConstantItem(120).AlignRight().Column(col =>
                    {
                        col.Item().Text(_employes.Count == 1 ? "Fiche individuelle" : $"{_employes.Count} employé(s)")
                            .FontSize(9).FontColor(Colors.Grey.Lighten3);
                        col.Item().Text(DateTime.UtcNow.ToString("dd/MM/yyyy")).FontSize(9).FontColor(Colors.Grey.Lighten3);
                    });
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Spacing(20);

                    foreach (var employe in _employes)
                    {
                        var affectationsEmploye = _affectations.Where(a => a.EmployeId == employe.Id).ToList();
                        var actives = affectationsEmploye.Where(a => a.DateRetrait == null).ToList();
                        var historique = affectationsEmploye.Where(a => a.DateRetrait != null).ToList();

                        // --- Encart identité + contact ---
                        col.Item().Border(1).BorderColor("#e5e7eb").Background("#f4f6f9").Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(identite =>
                            {
                                identite.Item().Text($"{employe.Prenom} {employe.Nom}").Bold().FontSize(13).FontColor("#0d3b66");
                                identite.Item().Text($"{employe.Poste ?? "Poste non renseigné"}").FontSize(9).FontColor(Colors.Grey.Darken1);
                                identite.Item().Text($"{employe.DepartementOrg?.Nom} — {employe.Service?.Nom}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            });
                            row.RelativeItem().AlignRight().Column(contact =>
                            {
                                contact.Item().Text(employe.Email).FontSize(9);
                                contact.Item().Text(employe.Telephone ?? "—").FontSize(9);
                                contact.Item().Text(employe.EstActif ? "Statut : Actif" : "Statut : Inactif").FontSize(9).Bold()
                                    .FontColor(employe.EstActif ? Colors.Green.Darken1 : Colors.Red.Darken1);
                            });
                        });

                        // --- Actifs actuellement utilisés ---
                        col.Item().PaddingTop(8).Text($"Actifs actuellement utilisés ({actives.Count})").Bold().FontSize(11).FontColor("#0d3b66");

                        if (actives.Any())
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(3);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#0d3b66").Padding(4).Text("Actif").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background("#0d3b66").Padding(4).Text("Catégorie").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background("#0d3b66").Padding(4).Text("Depuis le").FontColor(Colors.White).Bold().FontSize(9);
                                });

                                foreach (var aff in actives)
                                {
                                    table.Cell().Padding(4).Text($"{aff.Actif?.Nom} ({aff.Actif?.NumeroSerie})").FontSize(9);
                                    table.Cell().Padding(4).Text(aff.Actif?.CategorieActif?.Nom ?? "—").FontSize(9);
                                    table.Cell().Padding(4).Text(aff.DateAffectation.ToString("dd/MM/yyyy")).FontSize(9);
                                }
                            });
                        }
                        else
                        {
                            col.Item().Text("Aucun actif utilisé actuellement.").Italic().FontSize(9).FontColor(Colors.Grey.Darken1);
                        }

                        // --- Historique avec détenteur actuel ---
                        if (historique.Any())
                        {
                            col.Item().PaddingTop(8).Text($"Actifs utilisés auparavant ({historique.Count})").Bold().FontSize(11).FontColor("#0d3b66");

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(3);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(3);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#6b7280").Padding(4).Text("Actif").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background("#6b7280").Padding(4).Text("Du").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background("#6b7280").Padding(4).Text("Au").FontColor(Colors.White).Bold().FontSize(9);
                                    header.Cell().Background("#6b7280").Padding(4).Text("Détenteur actuel").FontColor(Colors.White).Bold().FontSize(9);
                                });

                                foreach (var aff in historique)
                                {
                                    var detenteurActuel = _detenteursActuels.TryGetValue(aff.ActifId, out var det) ? det : null;

                                    string texteDetenteur;
                                    if (detenteurActuel is null)
                                    {
                                        texteDetenteur = "Disponible (personne)";
                                    }
                                    else if (detenteurActuel.Id == employe.Id)
                                    {
                                        texteDetenteur = "Cet employé (réattribué)";
                                    }
                                    else
                                    {
                                        texteDetenteur = $"{detenteurActuel.Prenom} {detenteurActuel.Nom}";
                                    }

                                    table.Cell().Padding(4).Text(aff.Actif?.Nom ?? "—").FontSize(9);
                                    table.Cell().Padding(4).Text(aff.DateAffectation.ToString("dd/MM/yyyy")).FontSize(9);
                                    table.Cell().Padding(4).Text(aff.DateRetrait?.ToString("dd/MM/yyyy") ?? "—").FontSize(9);
                                    table.Cell().Padding(4).Text(texteDetenteur).FontSize(9)
                                        .FontColor(detenteurActuel is null ? Colors.Green.Darken1 : "#0d3b66");
                                }
                            });
                        }
                    }
                });

                page.Footer().Padding(10).Row(row =>
                {
                    row.RelativeItem().Text("EcoTrack — Système de gestion des actifs Ecobank Togo").FontSize(8).FontColor(Colors.Grey.Darken1);
                    row.RelativeItem().AlignRight().Text(x =>
                    {
                        x.Span("Généré le ").FontSize(8).FontColor(Colors.Grey.Darken1);
                        x.Span(DateTime.UtcNow.ToString("dd/MM/yyyy à HH:mm")).FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });
            });
        }
    }
}
