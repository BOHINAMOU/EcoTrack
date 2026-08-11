using System.ComponentModel.DataAnnotations;

namespace EcoTrack.ViewModels
{
    /// <summary>
    /// Utilisé par l'admin depuis "Tous les employés" pour modifier à la fois
    /// les informations de l'employé (poste, téléphone) et son compte (username, email).
    /// </summary>
    public class ModifierEmployeCompteViewModel
    {
        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(100)]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom d'utilisateur est obligatoire.")]
        [StringLength(50)]
        [Display(Name = "Nom d'utilisateur")]
        public string NomUtilisateur { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Poste { get; set; }

        [Required(ErrorMessage = "L'indicatif est obligatoire.")]
        public string Indicatif { get; set; } = "+228";

        [Required(ErrorMessage = "Le numéro de téléphone est obligatoire.")]
        [RegularExpression(@"^\d{6,10}$", ErrorMessage = "Le numéro doit contenir uniquement des chiffres (6 à 10).")]
        [Display(Name = "Numéro (sans l'indicatif)")]
        public string NumeroTelephone { get; set; } = string.Empty;
    }
}