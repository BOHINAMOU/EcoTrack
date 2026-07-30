using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EcoTrack.Enums;

namespace EcoTrack.Models
{
    public class Actif
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom de l'actif est obligatoire.")]
        [StringLength(150)]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le numéro de série est obligatoire.")]
        [StringLength(100)]
        [Display(Name = "Numéro de série")]
        public string NumeroSerie { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Marque { get; set; }

        [StringLength(100)]
        public string? Modele { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date d'acquisition")]
        public DateTime DateAcquisition { get; set; } = DateTime.UtcNow.Date;

        public EtatActif Etat { get; set; } = EtatActif.Disponible;

        [Required]
        [Display(Name = "Catégorie")]
        public int CategorieActifId { get; set; }
        public CategorieActif? CategorieActif { get; set; }

        [Display(Name = "Département / agence")]
        public int? DepartementId { get; set; }
        public Departement? Departement { get; set; }

        public ICollection<Affectation> Affectations { get; set; } = new List<Affectation>();
    }
}