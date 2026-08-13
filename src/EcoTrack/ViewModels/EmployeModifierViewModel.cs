using System.ComponentModel.DataAnnotations;
using EcoTrack.Models;

namespace EcoTrack.ViewModels
{
    public class EmployeModifierViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(100)]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'indicatif est obligatoire.")]
        [Display(Name = "Indicatif")]
        public string Indicatif { get; set; } = "+228";

        [Required(ErrorMessage = "Le numéro de téléphone est obligatoire.")]
        [RegularExpression(@"^\d{6,10}$", ErrorMessage = "Le numéro doit contenir uniquement des chiffres (6 à 10).")]
        [Display(Name = "Téléphone")]
        public string NumeroTelephone { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Poste { get; set; }

        [Required(ErrorMessage = "L'unité est obligatoire.")]
        [Display(Name = "Unité")]
        public int UniteId { get; set; }

        [Required]
        [Display(Name = "Agence")]
        public int AgenceId { get; set; }

        public List<Agence> Agences { get; set; } = new();
        public List<Departement> Departements { get; set; } = new();
        public List<Division> Divisions { get; set; } = new();
        public List<Service> Services { get; set; } = new();
        public List<Unite> Unites { get; set; } = new();

        public List<(string Code, string Pays)> IndicatifsListe { get; set; } = EcoTrack.Enums.Indicatifs.Liste;
    }
}