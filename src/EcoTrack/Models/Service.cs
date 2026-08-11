using System.ComponentModel.DataAnnotations;

namespace EcoTrack.Models
{
    /// <summary>
    /// Niveau 4 : le service, à l'intérieur d'une division.
    /// </summary>
    public class Service
    {
        public int Id { get; set; }


    [Required(ErrorMessage = "Le nom du service est obligatoire.")]
        [StringLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Display(Name = "Actif")]
        public bool EstActif { get; set; } = true;

        [Required]
        public int DivisionId { get; set; }

        public Division? Division { get; set; }

        public ICollection<Unite> Unites { get; set; } = new List<Unite>();
    }


}
