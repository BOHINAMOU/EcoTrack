using System.ComponentModel.DataAnnotations;

namespace EcoTrack.Models
{
    /// <summary>
    /// Un service appartient à un département/agence (ex: "Service Crédit" dans l'agence "ATAKPAME").
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
        [Display(Name = "Agence")]
        public int DepartementId { get; set; }
        public Departement? Departement { get; set; }

        public ICollection<Employe> Employes { get; set; } = new List<Employe>();
    }
}