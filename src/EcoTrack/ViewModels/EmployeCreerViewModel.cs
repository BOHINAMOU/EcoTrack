using System.ComponentModel.DataAnnotations;
using EcoTrack.Models;

namespace EcoTrack.ViewModels
{
    public enum ModeAttributionActif
    {
        ActifExistant,
        NouvelActif
    }

    public class EmployeCreerViewModel
    {
        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(100)]
        public string Prenom { get; set; } = string.Empty;
        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Le format de l'email n'est pas valide (ex: nom@domaine.com).")]
        [RegularExpression(@"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$", ErrorMessage = "Le format de l'email n'est pas valide (ex: nom@domaine.com).")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'indicatif est obligatoire.")]
        [Display(Name = "Indicatif")]
        public string Indicatif { get; set; } = "+228";

        [Required(ErrorMessage = "Le numéro de téléphone est obligatoire.")]
        [RegularExpression(@"^\d{6,10}$", ErrorMessage = "Le numéro doit contenir uniquement des chiffres (6 à 10).")]
        [Display(Name = "Numéro (sans l'indicatif)")]
        public string NumeroTelephone { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Poste { get; set; }

        [Required(ErrorMessage = "L'agence est obligatoire.")]
        [Display(Name = "Agence")]
        public int DepartementId { get; set; }
        [Required(ErrorMessage = "Le service est obligatoire.")]
        [Display(Name = "Service")]
        public int ServiceId { get; set; }

        public List<Service> Services { get; set; } = new();

        [Required]
        [Display(Name = "Mode d'attribution")]
        public ModeAttributionActif ModeAttribution { get; set; } = ModeAttributionActif.ActifExistant;

        [Display(Name = "Actif existant à attribuer")]
        public int? ActifExistantId { get; set; }

        [StringLength(150)]
        [Display(Name = "Nom de l'actif")]
        public string? NouvelActifNom { get; set; }

        [StringLength(100)]
        [Display(Name = "Numéro de série")]
        public string? NouvelActifNumeroSerie { get; set; }

        [StringLength(100)]
        public string? NouvelActifMarque { get; set; }

        [StringLength(100)]
        public string? NouvelActifModele { get; set; }

        [Display(Name = "Catégorie")]
        public int? NouvelActifCategorieId { get; set; }

        public List<Departement> Departements { get; set; } = new();
        public List<Actif> ActifsDisponibles { get; set; } = new();
        public List<CategorieActif> Categories { get; set; } = new();
        public List<(string Code, string Pays)> Indicatifs { get; set; } = EcoTrack.Enums.Indicatifs.Liste;
    }
}