using System.ComponentModel.DataAnnotations;

namespace EcoTrack.ViewModels
{
    public class ProfilViewModel
    {
        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(100)]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom d'utilisateur est obligatoire.")]
        [StringLength(50)]
        [Display(Name = "Nom d'utilisateur")]
        public string NomUtilisateur { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}