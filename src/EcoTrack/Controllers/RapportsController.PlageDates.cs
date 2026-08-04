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
        // GET /Rapports/PlageDates
        [HttpGet]
        public IActionResult PlageDates()
        {
            return View(new PlageDatesViewModel
            {
                DateDebut = DateTime.UtcNow.AddMonths(-1).Date,
                DateFin = DateTime.UtcNow.Date
            });
        }

        // POST /Rapports/PlageDates
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlageDates(PlageDatesViewModel model)
        {
            if (model.DateDebut > model.DateFin)
            {
                ModelState.AddModelError(string.Empty, "La date de début doit précéder la date de fin.");
                return View(model);
            }

            var dateDebutUtc = DateTime.SpecifyKind(model.DateDebut.Date, DateTimeKind.Utc);
            var dateFinInclusive = DateTime.SpecifyKind(model.DateFin.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

            var actifsAcquis = await _context.Actifs
                .Include(a => a.CategorieActif)
                .Where(a => a.DateAcquisition >= dateDebutUtc && a.DateAcquisition <= dateFinInclusive)
                .OrderBy(a => a.DateAcquisition)
                .ToListAsync();

            var affectationsRequete = _context.Affectations
                .Include(a => a.Actif)
                .Include(a => a.Employe)
                .Where(a => a.DateAffectation >= dateDebutUtc && a.DateAffectation <= dateFinInclusive)
                .AsQueryable();

            if (model.EmployeId.HasValue)
            {
                affectationsRequete = affectationsRequete.Where(a => a.EmployeId == model.EmployeId.Value);
            }

            var affectationsPeriode = await affectationsRequete.OrderBy(a => a.DateAffectation).ToListAsync();

            string? nomEmployeFiltre = null;
            if (model.EmployeId.HasValue)
            {
                var employeFiltre = await _context.Employes.FindAsync(model.EmployeId.Value);
                nomEmployeFiltre = employeFiltre is not null ? $"{employeFiltre.Prenom} {employeFiltre.Nom}" : null;
            }

            // Détenteur actuel de chaque actif concerné par la période
            var actifIdsPeriode = affectationsPeriode.Select(a => a.ActifId).Distinct().ToList();
            var detenteursActuels = await _context.Affectations
                .Include(a => a.Employe)
                .Where(a => actifIdsPeriode.Contains(a.ActifId) && a.DateRetrait == null)
                .ToDictionaryAsync(a => a.ActifId, a => a.Employe);

            var document = new RapportPlageDatesDocument(model.DateDebut, model.DateFin, actifsAcquis, affectationsPeriode, nomEmployeFiltre, detenteursActuels);
            var pdf = document.GeneratePdf();

            var nomFichier = $"Rapport_Periode_{model.DateDebut:yyyyMMdd}_{model.DateFin:yyyyMMdd}.pdf";
            return File(pdf, "application/pdf", nomFichier);
        }
    }

    public class PlageDatesViewModel
    {
        [System.ComponentModel.DataAnnotations.Display(Name = "Date de début")]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public DateTime DateDebut { get; set; }

        [System.ComponentModel.DataAnnotations.Display(Name = "Date de fin")]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public DateTime DateFin { get; set; }

        public int? EmployeId { get; set; }
        public string? EmployeNomComplet { get; set; }
    }

    public class RapportPlageDatesDocument : IDocument
    {
        private readonly DateTime _dateDebut;
        private readonly DateTime _dateFin;
        private readonly List<Actif> _actifsAcquis;
        private readonly List<Affectation> _affectations;
        private readonly string? _nomEmployeFiltre;
        private readonly Dictionary<int, Employe?> _detenteursActuels;

        public RapportPlageDatesDocument(DateTime dateDebut, DateTime dateFin, List<Actif> actifsAcquis, List<Affectation> affectations, string? nomEmployeFiltre, Dictionary<int, Employe?> detenteursActuels)
        {
            _dateDebut = dateDebut;
            _dateFin = dateFin;
            _actifsAcquis = actifsAcquis;
            _affectations = affectations;
            _nomEmployeFiltre = nomEmployeFiltre;
            _detenteursActuels = detenteursActuels;
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
                    col.Item().Text($"Rapport du {_dateDebut:dd/MM/yyyy} au {_dateFin:dd/MM/yyyy}").FontSize(11).FontColor(Colors.Grey.Darken1);
                    if (_nomEmployeFiltre is not null)
                    {
                        col.Item().Text($"Filtré pour : {_nomEmployeFiltre}").FontSize(10).FontColor("#0d3b66").Bold();
                    }
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor("#0d3b66");
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Spacing(15);

                    col.Item().Text($"Actifs acquis sur la période ({_actifsAcquis.Count})").Bold().FontSize(12);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Nom").Bold();
                            header.Cell().Text("N° série").Bold();
                            header.Cell().Text("Catégorie").Bold();
                            header.Cell().Text("Date").Bold();
                        });

                        foreach (var actif in _actifsAcquis)
                        {
                            table.Cell().Text(actif.Nom);
                            table.Cell().Text(actif.NumeroSerie);
                            table.Cell().Text(actif.CategorieActif?.Nom ?? "—");
                            table.Cell().Text(actif.DateAcquisition.ToString("dd/MM/yyyy"));
                        }
                    });

                    if (!_actifsAcquis.Any())
                    {
                        col.Item().Text("Aucun actif acquis sur cette période.").Italic().FontColor(Colors.Grey.Darken1);
                    }

                    col.Item().PaddingTop(10).Text($"Affectations survenues sur la période ({_affectations.Count})").Bold().FontSize(12);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Actif").Bold();
                            header.Cell().Text("Employé").Bold();
                            header.Cell().Text("Date").Bold();
                            header.Cell().Text("Détenteur actuel").Bold();
                        });

                        foreach (var affectation in _affectations)
                        {
                            var detenteurActuel = _detenteursActuels.TryGetValue(affectation.ActifId, out var det) ? det : null;
                            string texteDetenteur = detenteurActuel is null
                                ? "Disponible"
                                : (detenteurActuel.Id == affectation.EmployeId ? "Le même" : $"{detenteurActuel.Prenom} {detenteurActuel.Nom}");

                            table.Cell().Text(affectation.Actif?.Nom ?? "—");
                            table.Cell().Text($"{affectation.Employe?.Prenom} {affectation.Employe?.Nom}");
                            table.Cell().Text(affectation.DateAffectation.ToString("dd/MM/yyyy"));
                            table.Cell().Text(texteDetenteur);
                        }
                    });

                    if (!_affectations.Any())
                    {
                        col.Item().Text("Aucune affectation sur cette période.").Italic().FontColor(Colors.Grey.Darken1);
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