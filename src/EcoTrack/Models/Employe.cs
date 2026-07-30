using System.ComponentModel.DataAnnotations;

namespace EcoTrack.Models
{
    public class Employe
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(100)]
        public string Prenom { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Le format du numéro de téléphone n'est pas valide.")]
        [StringLength(20)]
        [Display(Name = "Numéro de téléphone")]
        public string? Telephone { get; set; }

        [StringLength(100)]
        public string? Poste { get; set; }

        [Display(Name = "Employé actif")]
        public bool EstActif { get; set; } = true;

        [Required]
        [Display(Name = "Département / agence")]
        public int DepartementId { get; set; }
        public Departement? Departement { get; set; }

        public ICollection<Affectation> Affectations { get; set; } = new List<Affectation>();
    }
}