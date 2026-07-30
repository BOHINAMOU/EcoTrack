using System.ComponentModel.DataAnnotations;

namespace EcoTrack.Models
{
    public class CategorieActif
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom de la catégorie est obligatoire.")]
        [StringLength(100)]
        public string Nom { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }
        [Display(Name = "Actif")]
        public bool EstActif { get; set; } = true;

        public ICollection<Actif> Actifs { get; set; } = new List<Actif>();
    }
}