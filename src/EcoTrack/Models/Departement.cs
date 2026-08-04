using System.ComponentModel.DataAnnotations;

namespace EcoTrack.Models
{
    public class Departement
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(150)]
        [Display(Name = "Nom de l'agence")]
        public string Nom { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Code { get; set; }

        [StringLength(150)]
        public string? Localisation { get; set; }

        [Display(Name = "Actif")]
        public bool EstActif { get; set; } = true;

        public ICollection<Employe> Employes { get; set; } = new List<Employe>();
        public ICollection<Actif> Actifs { get; set; } = new List<Actif>();
        public ICollection<Service> Services { get; set; } = new List<Service>();
    }
}