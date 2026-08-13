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
        [EmailAddress(ErrorMessage = "Le format de l'email n'est pas valide.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'indicatif est obligatoire.")]
        [Display(Name = "Indicatif")]
        public string Indicatif { get; set; } = "+228";

        [Required(ErrorMessage = "Le numéro de téléphone est obligatoire.")]
        [RegularExpression(@"^\d{6,10}$", ErrorMessage = "Le numéro doit contenir uniquement des chiffres (6 à 10).")]
        [Display(Name = "Téléphone:")]
        public string NumeroTelephone { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Poste { get; set; }

        [StringLength(50)]
        [Display(Name = "Nom d'utilisateur (laisser vide pour générer automatiquement)")]
        public string? NomUtilisateur { get; set; }

        // --- Chaîne organisationnelle : seul UniteId est réellement enregistré,
        // les autres ne servent qu'à alimenter les menus en cascade côté vue. ---
        [Required(ErrorMessage = "L'unité est obligatoire.")]
        [Display(Name = "Unité")]
        public int UniteId { get; set; }

        [Required]
        [Display(Name = "Agence")]
        public int AgenceId { get; set; }

        // --- Attribution obligatoire d'un actif ---
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

        // --- Listes pour remplir les menus déroulants en cascade ---
        public List<Agence> Agences { get; set; } = new();
        public List<Departement> Departements { get; set; } = new();
        public List<Division> Divisions { get; set; } = new();
        public List<Service> Services { get; set; } = new();
        public List<Unite> Unites { get; set; } = new();

        public List<Actif> ActifsDisponibles { get; set; } = new();
        public List<CategorieActif> Categories { get; set; } = new();
        public List<(string Code, string Pays)> IndicatifsListe { get; set; } = EcoTrack.Enums.Indicatifs.Liste;
    }
}