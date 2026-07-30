using System.ComponentModel.DataAnnotations;

namespace EcoTrack.ViewModels
{
    public class ChangerMotDePasseViewModel
    {
        [Required(ErrorMessage = "Le mot de passe actuel est obligatoire.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe actuel")]
        public string MotDePasseActuel { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nouveau mot de passe")]
        public string NouveauMotDePasse { get; set; } = string.Empty;

        [Required(ErrorMessage = "Veuillez confirmer le nouveau mot de passe.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le nouveau mot de passe")]
        [Compare(nameof(NouveauMotDePasse), ErrorMessage = "Les mots de passe ne correspondent pas.")]
        public string ConfirmationMotDePasse { get; set; } = string.Empty;
    }
}