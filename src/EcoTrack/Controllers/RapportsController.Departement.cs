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
            ViewBag.Agences = await _context.Departements.OrderBy(d => d.Nom).ToListAsync();
            ViewBag.Services = await _context.Services
                .Select(s => new { s.Id, s.Nom, s.DepartementId })
                .ToListAsync();

            return View();
        }

        // POST /Rapports/Departement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Departement(int? departementId, int? serviceId)
        {
            if (!departementId.HasValue && !serviceId.HasValue)
            {
                TempData["Erreur"] = "Choisissez une agence ou un service.";
                return RedirectToAction(nameof(Departement));
            }

            List<Employe> employes;
            string titre;

            if (serviceId.HasValue)
            {
                var service = await _context.Services
                    .Include(s => s.Departement)
                    .Include(s => s.Employes)
                    .FirstOrDefaultAsync(s => s.Id == serviceId.Value);

                if (service is null)
                {
                    TempData["Erreur"] = "Service introuvable.";
                    return RedirectToAction(nameof(Departement));
                }

                employes = service.Employes.ToList();
                titre = $"{service.Departement?.Nom} — Service {service.Nom}";
            }
            else
            {
                var departement = await _context.Departements
                    .Include(d => d.Employes)
                    .FirstOrDefaultAsync(d => d.Id == departementId!.Value);

                if (departement is null)
                {
                    TempData["Erreur"] = "Agence introuvable.";
                    return RedirectToAction(nameof(Departement));
                }

                employes = departement.Employes.ToList();
                titre = departement.Nom;
            }

            var employeIds = employes.Select(e => e.Id).ToList();

            var affectationsActives = await _context.Affectations
                .Include(a => a.Actif)
                    .ThenInclude(a => a!.CategorieActif)
                .Include(a => a.Employe)
                .Where(a => employeIds.Contains(a.EmployeId) && a.DateRetrait == null)
                .ToListAsync();

            var document = new RapportDepartementDocument(titre, employes, affectationsActives);
            var pdf = document.GeneratePdf();

            return File(pdf, "application/pdf", $"Rapport_{titre.Replace(" ", "_")}.pdf");
        }
    }

    public class RapportDepartementDocument : IDocument
    {
        private readonly string _titre;
        private readonly List<Employe> _employes;
        private readonly List<Affectation> _affectationsActives;

        public RapportDepartementDocument(string titre, List<Employe> employes, List<Affectation> affectationsActives)
        {
            _titre = titre;
            _employes = employes;
            _affectationsActives = affectationsActives;
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

                    col.Item().Text($"{_employes.Count} employé(s) — {_affectationsActives.Count} actif(s) attribué(s)")
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