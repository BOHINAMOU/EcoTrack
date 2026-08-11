using System.ComponentModel.DataAnnotations;
using EcoTrack.Models;

namespace EcoTrack.ViewModels
{
    public class ActifFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire.")]
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

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date d'acquisition")]
        public DateTime DateAcquisition { get; set; } = DateTime.UtcNow.Date;

        [Required(ErrorMessage = "La catégorie est obligatoire.")]
        [Display(Name = "Catégorie")]
        public int CategorieActifId { get; set; }

        /// <summary>Format "EM:12" (employé), "AG:3" (agence), "DP:5" (département), "DV:2" (division), "SV:9" (service), "UN:4" (unité), ou vide (aucun/disponible).</summary>
        [Display(Name = "Attribuer à")]
        public string? ProprietaireUnite { get; set; }

        public List<CategorieActif> Categories { get; set; } = new();
        public List<Employe> Employes { get; set; } = new();
        public List<Agence> Agences { get; set; } = new();
        public List<Departement> Departements { get; set; } = new();
        public List<Division> Divisions { get; set; } = new();
        public List<Service> Services { get; set; } = new();
        public List<Unite> Unites { get; set; } = new();
    }
}