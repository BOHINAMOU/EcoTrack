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

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [Required]
        [Display(Name = "Indicatif")]
        public string Indicatif { get; set; } = "+228";

        [Required]
        [Display(Name = "Téléphone")]
        [StringLength(20)]
        public string NumeroTelephone { get; set; } = string.Empty;

        public List<(string Code, string Pays)> Indicatifs { get; set; } = new();

        [StringLength(100)]
        public string? Poste { get; set; }

        [Required(ErrorMessage = "Le département est obligatoire.")]
        [Display(Name = "Département / agence")]
        public int DepartementId { get; set; }

        public List<Departement> Departements { get; set; } = new();
    }
}