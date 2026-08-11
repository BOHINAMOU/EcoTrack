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

        [Display(Name = "Agence (si actif partagé)")]
        public int? AgenceId { get; set; }
        public Agence? Agence { get; set; }

        [Display(Name = "Département (si actif partagé)")]
        public int? DepartementId { get; set; }
        public Departement? Departement { get; set; }

        [Display(Name = "Division (si actif partagé)")]
        public int? DivisionId { get; set; }
        public Division? Division { get; set; }

        [Display(Name = "Service (si actif partagé)")]
        public int? ServiceId { get; set; }
        public Service? Service { get; set; }

        [Display(Name = "Unité (si actif partagé)")]
        public int? UniteId { get; set; }
        public Unite? Unite { get; set; }

        /// <summary>Vrai si l'actif appartient à une unité organisationnelle plutôt qu'à un employé précis.</summary>
        [NotMapped]
        public bool EstPartageAUneUnite => AgenceId is not null || DepartementId is not null || DivisionId is not null || ServiceId is not null || UniteId is not null;

        public ICollection<Affectation> Affectations { get; set; } = new List<Affectation>();
    }
}