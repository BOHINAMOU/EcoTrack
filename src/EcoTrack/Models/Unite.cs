using System.ComponentModel.DataAnnotations;

namespace EcoTrack.Models
{
    /// <summary>Niveau 5 (le plus fin) : l'unité, à l'intérieur d'un service. Les employés y sont rattachés directement.</summary>
    public class Unite
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(150)]
        public string Nom { get; set; } = string.Empty;

        [Display(Name = "Actif")]
        public bool EstActif { get; set; } = true;

        [Required]
        public int ServiceId { get; set; }
        public Service? Service { get; set; }

        public ICollection<Employe> Employes { get; set; } = new List<Employe>();
    }
}