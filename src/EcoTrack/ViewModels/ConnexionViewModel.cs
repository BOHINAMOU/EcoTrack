using System.ComponentModel.DataAnnotations;

namespace EcoTrack.ViewModels
{
    public class ConnexionViewModel
    {
        [Required(ErrorMessage = "Le nom d'utilisateur est obligatoire.")]
        [Display(Name = "Nom d'utilisateur")]
        public string NomUtilisateur { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string MotDePasse { get; set; } = string.Empty;

        [Display(Name = "Se souvenir de moi")]
        public bool SeSouvenirDeMoi { get; set; }
    }
}