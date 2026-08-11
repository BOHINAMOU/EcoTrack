using EcoTrack.Enums;
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
        // GET /Rapports/Inventaire
        [HttpGet]
        public async Task<IActionResult> Inventaire()
        {
            ViewBag.Categories = await _context.CategoriesActifs.OrderBy(c => c.Nom).ToListAsync();
            return View();
        }

        // POST /Rapports/Inventaire
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Inventaire(int? categorieId, string? etat)
        {
            var requete = _context.Actifs
                .Include(a => a.CategorieActif)
                .AsQueryable();

            if (categorieId.HasValue)
            {
                requete = requete.Where(a => a.CategorieActifId == categorieId.Value);
            }

            EtatActif? etatFiltre = null;
            if (!string.IsNullOrWhiteSpace(etat) && Enum.TryParse<EtatActif>(etat, true, out var etatParse))
            {
                etatFiltre = etatParse;
                requete = requete.Where(a => a.Etat == etatParse);
            }

            var actifs = await requete.OrderBy(a => a.CategorieActif!.Nom).ThenBy(a => a.Nom).ToListAsync();

            var actifIds = actifs.Select(a => a.Id).ToList();
            var detenteursActuels = await _context.Affectations
                .Include(a => a.Employe)
                .Where(a => actifIds.Contains(a.ActifId) && a.DateRetrait == null)
                .ToDictionaryAsync(a => a.ActifId, a => a.Employe);

            string? nomCategorieFiltre = categorieId.HasValue
                ? (await _context.CategoriesActifs.FindAsync(categorieId.Value))?.Nom
                : null;

            string? libelleEtatFiltre = etatFiltre switch
            {
                EtatActif.Disponible => "Disponible",
                EtatActif.Attribue => "Attribué",
                EtatActif.Deteriore => "Détérioré",
                EtatActif.EnPanne => "En panne",
                _ => null
            };

            var document = new RapportInventaireDocument(actifs, detenteursActuels, nomCategorieFiltre, libelleEtatFiltre);
            var pdf = document.GeneratePdf();

            return File(pdf, "application/pdf", "Rapport_Inventaire_Actifs.pdf");
        }
    }

    public class RapportInventaireDocument : IDocument
    {
        private readonly List<Actif> _actifs;
        private readonly Dictionary<int, Employe?> _detenteursActuels;
        private readonly string? _nomCategorieFiltre;
        private readonly string? _libelleEtatFiltre;

        public RapportInventaireDocument(List<Actif> actifs, Dictionary<int, Employe?> detenteursActuels, string? nomCategorieFiltre, string? libelleEtatFiltre)
        {
            _actifs = actifs;
            _detenteursActuels = detenteursActuels;
            _nomCategorieFiltre = nomCategorieFiltre;
            _libelleEtatFiltre = libelleEtatFiltre;
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Background("#0d3b66").Padding(15).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("INVENTAIRE DES ACTIFS").FontSize(16).Bold().FontColor(Colors.White);
                        col.Item().Text("EcoTrack — Ecobank Togo").FontSize(9).FontColor(Colors.Grey.Lighten3);
                    });
                    row.ConstantItem(140).AlignRight().Column(col =>
                    {
                        col.Item().Text($"{_actifs.Count} actif(s)").FontSize(9).FontColor(Colors.Grey.Lighten3);
                        col.Item().Text(DateTime.UtcNow.ToString("dd/MM/yyyy")).FontSize(9).FontColor(Colors.Grey.Lighten3);
                    });
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    if (_nomCategorieFiltre is not null || _libelleEtatFiltre is not null)
                    {
                        var filtres = new List<string>();
                        if (_nomCategorieFiltre is not null) filtres.Add($"Catégorie : {_nomCategorieFiltre}");
                        if (_libelleEtatFiltre is not null) filtres.Add($"État : {_libelleEtatFiltre}");
                        col.Item().Text(string.Join("  |  ", filtres)).FontSize(10).Bold().FontColor("#0d3b66");
                        col.Item().PaddingBottom(10);
                    }

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(3);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background("#0d3b66").Padding(4).Text("Nom").FontColor(Colors.White).Bold();
                            header.Cell().Background("#0d3b66").Padding(4).Text("N° série").FontColor(Colors.White).Bold();
                            header.Cell().Background("#0d3b66").Padding(4).Text("Catégorie").FontColor(Colors.White).Bold();
                            header.Cell().Background("#0d3b66").Padding(4).Text("Acquis le").FontColor(Colors.White).Bold();
                            header.Cell().Background("#0d3b66").Padding(4).Text("État").FontColor(Colors.White).Bold();
                            header.Cell().Background("#0d3b66").Padding(4).Text("Détenteur actuel").FontColor(Colors.White).Bold();
                        });

                        foreach (var actif in _actifs)
                        {
                            var detenteur = _detenteursActuels.TryGetValue(actif.Id, out var d) ? d : null;

                            var texteEtat = actif.Etat switch
                            {
                                EtatActif.Disponible => "Disponible",
                                EtatActif.Attribue => "Attribué",
                                EtatActif.Deteriore => "Détérioré",
                                EtatActif.EnPanne => "En panne",
                                _ => "—"
                            };

                            string couleurEtat = actif.Etat switch
                            {
                                EtatActif.Disponible => Colors.Green.Darken1,
                                EtatActif.Attribue => "#0d3b66",
                                EtatActif.Deteriore => Colors.Red.Darken1,
                                EtatActif.EnPanne => "#212529",
                                _ => "#000000"
                            };

                            table.Cell().Padding(4).Text(actif.Nom);
                            table.Cell().Padding(4).Text(actif.NumeroSerie);
                            table.Cell().Padding(4).Text(actif.CategorieActif?.Nom ?? "—");
                            table.Cell().Padding(4).Text(actif.DateAcquisition.ToString("dd/MM/yyyy"));
                            table.Cell().Padding(4).Text(texteEtat).FontColor(couleurEtat).Bold();
                            table.Cell().Padding(4).Text(detenteur is not null ? $"{detenteur.Prenom} {detenteur.Nom}" : "—");
                        }
                    });

                    if (!_actifs.Any())
                    {
                        col.Item().PaddingTop(10).Text("Aucun actif ne correspond à ces critères.").Italic().FontColor(Colors.Grey.Darken1);
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