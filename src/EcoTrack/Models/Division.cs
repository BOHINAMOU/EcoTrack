using System.ComponentModel.DataAnnotations;

namespace EcoTrack.Models
{
    /// <summary>Niveau 3 : la division, à l'intérieur d'un département.</summary>
    public class Division
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(150)]
        public string Nom { get; set; } = string.Empty;

        [Display(Name = "Actif")]
        public bool EstActif { get; set; } = true;

        [Required]
        public int DepartementId { get; set; }
        public Departement? Departement { get; set; }

        public ICollection<Service> Services { get; set; } = new List<Service>();
    }
}