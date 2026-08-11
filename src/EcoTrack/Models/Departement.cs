using System.ComponentModel.DataAnnotations;

namespace EcoTrack.Models
{
    /// <summary>Niveau 2 : le département, à l'intérieur d'une agence.</summary>
    public class Departement
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(150)]
        public string Nom { get; set; } = string.Empty;

        [Display(Name = "Actif")]
        public bool EstActif { get; set; } = true;

        [Required]
        public int AgenceId { get; set; }
        public Agence? Agence { get; set; }

        public ICollection<Division> Divisions { get; set; } = new List<Division>();
    }
}