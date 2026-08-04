using System.ComponentModel.DataAnnotations;
using EcoTrack.Models;

namespace EcoTrack.ViewModels
{
    public class EmployeModifierViewModel
    {
        [Required(ErrorMessage = "Le service est obligatoire.")]
        [Display(Name = "Service")]
        public int ServiceId { get; set; }

        public List<Service> Services { get; set; } = new();
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

        [Required(ErrorMessage = "L'agence est obligatoire.")]
        [Display(Name = "Agence")]
        public int DepartementId { get; set; }

        public List<Departement> Departements { get; set; } = new();
    }
}